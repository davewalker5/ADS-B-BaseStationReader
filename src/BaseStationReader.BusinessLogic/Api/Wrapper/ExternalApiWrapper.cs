using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    internal class ExternalApiWrapper : IExternalApiWrapper
    {
        private readonly int _maximumLookupAttempts;
        private readonly ITrackerLogger _logger;
        private readonly IExternalApiRegister _register;
        private readonly IActiveFlightApiWrapper _activeFlightWrapper;
        private readonly IHistoricalFlightApiWrapper _historicalFlightWrapper;
        private readonly IAirlineApiWrapper _airlineApiWrapper;
        private readonly IAircraftApiWrapper _aircraftApiWrapper;
        private readonly IAirportWeatherApiWrapper _airportWeatherApiWrapper;
        private readonly IDatabaseManagementFactory _factory;
        private readonly ITrackedAircraftWriter _trackedAircraftWriter;
        private readonly IFlightNumberApiWrapper _flightNumberApiWrapper;

        public ExternalApiWrapper(
            int maximumLookupAttempts,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            ITrackedAircraftWriter trackedAircraftWriter)
        {
            _maximumLookupAttempts = maximumLookupAttempts;
            _logger = logger;
            _factory = factory;
            _register = new ExternalApiRegister(logger);
            _airlineApiWrapper = new AirlineApiWrapper(logger, _register, _factory.AirlineManager);
            _activeFlightWrapper = new ActiveFlightApiWrapper(logger, _register, _airlineApiWrapper, _factory.FlightManager);
            _historicalFlightWrapper = new HistoricalFlightApiWrapper(logger, _register, _airlineApiWrapper, _factory.FlightManager, trackedAircraftWriter);
            _aircraftApiWrapper = new AircraftApiWrapper(logger, _register, _factory.AircraftManager, _factory.ModelManager, _factory.ManufacturerManager);
            _airportWeatherApiWrapper = new AirportWeatherApiWrapper(logger, _register);
            _flightNumberApiWrapper = new FlightNumberApiWrapper(_logger, factory, trackedAircraftWriter);
            _trackedAircraftWriter = trackedAircraftWriter;
        }

        /// <summary>
        /// Register an external API instance
        /// </summary>
        /// <param name="type"></param>
        /// <param name="api"></param>
        public void RegisterExternalApi(ApiEndpointType type, IExternalApi api)
            => _register.RegisterExternalApi(type, api);

        /// <summary>
        /// Return a flight number given a callsign
        /// </summary>
        /// <param name="callsign"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public async Task<FlightNumber> GetFlightNumberFromCallsignAsync(string callsign, DateTime? timestamp = null)
            => await _flightNumberApiWrapper.GetFlightNumberFromCallsignAsync(callsign, timestamp);

        /// <summary>
        /// Return a flight number for each callsign in the supplied list
        /// </summary>
        /// <param name="callsigns"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public async Task<List<FlightNumber>> GetFlightNumbersFromCallsigns(IEnumerable<string> callsigns, DateTime? timestamp = null)
            => await _flightNumberApiWrapper.GetFlightNumbersFromCallsigns(callsigns);

        /// <summary>
        /// Get flight numbers for aircraft that are currently being tracked
        /// </summary>
        /// <param name="statuses"></param>
        /// <returns></returns>
        public async Task<List<FlightNumber>> GetFlightNumbersForTrackedAircraftAsync(IEnumerable<TrackingStatus> statuses)
            => await _flightNumberApiWrapper.GetFlightNumbersForTrackedAircraftAsync(statuses);

        /// <summary>
        /// Lookup a flight and aircraft given a 24-bit aircraft ICAO address and filtering parameters
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirports"></param>
        /// <param name="arrivalAirports"></param>
        /// <returns></returns>
        public async Task<bool> LookupAsync(
            ApiEndpointType type,
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes,
            bool createSighting)
        {
            // Check the aircraft is eligible for lookup
            var eligible = await IsEligibleForLookup(address);
            if (!eligible)
            {
                return false;
            }

            // Lookup the aircraft
            var aircraft = await _aircraftApiWrapper.LookupAircraftAsync(address, "");

            // Lookup the flight
            var flight = await LookupFlightAsync(type, address, aircraft.Callsign, departureAirportCodes, arrivalAirportCodes);

            // The lookup is considered successful if the aircraft and flight are valid
            var successful = (aircraft != null) && (flight != null);

            // Update the lookup properties on the tracked aircraft record
            if (_trackedAircraftWriter != null)
            {
                await _trackedAircraftWriter.UpdateLookupProperties(address, successful, _maximumLookupAttempts);
            }

            // If the lookup was successful and sighting creation is requested, save the relationship
            // between the flight and the aircraft as a sighting on this date
            if (createSighting && successful)
            {
                _ = await _factory.SightingManager.AddAsync(aircraft.Id, flight.Id, DateTime.Today);
            }

            // The lookup was successful if both aircraft and flight were looked up successfully
            return successful;
        }

        /// <summary>
        /// Lookup all active flights within a bounding box around a central point
        /// </summary>
        /// <param name="centreLatitude"></param>
        /// <param name="centreLongitude"></param>
        /// <param name="rangeNm"></param>
        /// <returns></returns>
        public async Task<List<Flight>> LookupActiveFlightsInBoundingBox(
            double centreLatitude,
            double centreLongitude,
            double rangeNm)
            => await _activeFlightWrapper.LookupFlightsInBoundingBox(centreLatitude, centreLongitude, rangeNm);

        /// <summary>
        /// Lookup the current weather for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupCurrentAirportWeather(string icao)
            => await _airportWeatherApiWrapper.LookupCurrentAirportWeather(icao);

        /// <summary>
        /// Lookup the weather forecast for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupAirportWeatherForecast(string icao)
            => await _airportWeatherApiWrapper.LookupAirportWeatherForecast(icao);

        /// <summary>
        /// Lookup a flight, detecting the right API instance and key for flight lookup
        /// </summary>
        /// <param name="type"></param>
        /// <param name="address"></param>
        /// <param name="callsign"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        private async Task<Flight> LookupFlightAsync(
            ApiEndpointType type,
            string address,
            string callsign,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            Flight flight;

            // If it's a historical flight, use the historical API instance and the aircraft address
            if (type == ApiEndpointType.HistoricalFlights)
            {
                flight = await _historicalFlightWrapper.LookupFlightAsync(ApiProperty.AircraftAddress, address, address, departureAirportCodes, arrivalAirportCodes);
                return flight;
            }

            // It's an active flight, so try lookup by aircraft address first. This will return immediately if aircraft
            // address lookup isn't supported by the API instance
            flight = await _activeFlightWrapper.LookupFlightAsync(ApiProperty.AircraftAddress, address, address, departureAirportCodes, arrivalAirportCodes);
            if (flight != null)
            {
                return flight;
            }

            // Lookup by aircraft address isn't supported, so we need a valid callsign to do flight number to callsign
            // mapping
            if (string.IsNullOrEmpty(callsign))
            {
                return null;
            }

            // Lookup the flight number based on the callsign and check it's found
            var flightNumber = await _flightNumberApiWrapper.GetFlightNumberFromCallsignAsync(callsign);
            if (flightNumber != null)
            {
                return null;
            }

            // Lookup the flight by number
            flight = await _activeFlightWrapper.LookupFlightAsync(ApiProperty.FlightNumber, flightNumber.Number, address, departureAirportCodes, arrivalAirportCodes);
            return flight;
        }

        /// <summary>
        /// Check that an aircraft is eligible for lookup
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private async Task<bool> IsEligibleForLookup(string address)
        {
            // If the tracked aircraft writer isn't specified, just allow the lookup
            if (_trackedAircraftWriter == null)
            {
                return true;
            }

            // Look for a tracked aircraft with the specified address and  no lookup timestamp
            var aircraft = await _trackedAircraftWriter.GetAsync(x => (x.Address == address) && (x.LookupTimestamp == null));
            if (aircraft == null)
            {
                _logger.LogMessage(Severity.Warning, $"Aircraft {address} is not tracked or has already been looked up");
                return false;
            }

            // Check the maximum number of attempts hasn't been reached. A maximum of 0 indicates unlimited attempts
            if ((_maximumLookupAttempts > 0) && (aircraft.LookupAttempts >= _maximumLookupAttempts))
            {
                _logger.LogMessage(Severity.Warning, $"Aircraft {address} has reached the maximum lookup attempts");
                return false;
            }

            return true;
        }
    }
}