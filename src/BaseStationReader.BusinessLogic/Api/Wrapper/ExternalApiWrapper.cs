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
        private readonly IActiveFlightApiWrapper _activeFlightApiWrapper;
        private readonly IHistoricalFlightApiWrapper _historicalFlightWrapper;
        private readonly IAirlineApiWrapper _airlineApiWrapper;
        private readonly IAircraftApiWrapper _aircraftApiWrapper;
        private readonly IAirportWeatherApiWrapper _airportWeatherApiWrapper;
        private readonly IDatabaseManagementFactory _factory;
        private readonly ITrackedAircraftWriter _trackedAircraftWriter;
        private readonly IFlightNumberApiWrapper _flightNumberApiWrapper;
        private readonly ILookupEligibilityAssessor _lookupEligibilityAssessor;

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
            _activeFlightApiWrapper = new ActiveFlightApiWrapper(logger, _register, _airlineApiWrapper, _factory.FlightManager);
            _historicalFlightWrapper = new HistoricalFlightApiWrapper(logger, _register, _airlineApiWrapper, _factory.FlightManager, trackedAircraftWriter);
            _aircraftApiWrapper = new AircraftApiWrapper(logger, _register, _factory.AircraftManager, _factory.ModelManager, _factory.ManufacturerManager);
            _airportWeatherApiWrapper = new AirportWeatherApiWrapper(logger, _register);
            _flightNumberApiWrapper = new FlightNumberApiWrapper(_logger, factory, trackedAircraftWriter);
            _trackedAircraftWriter = trackedAircraftWriter;
            _lookupEligibilityAssessor = new LookupEligibilityAssessor(logger, _historicalFlightWrapper, _activeFlightApiWrapper, _factory, _trackedAircraftWriter, maximumLookupAttempts);
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
        public async Task<LookupResult> LookupAsync(
            ApiEndpointType type,
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes,
            bool createSighting)
        {
            // Check the aircraft is eligible for lookup
            var eligible = await _lookupEligibilityAssessor.IsEligibleForLookup(type, address);
            if (!eligible.Eligible)
            {
                // An aircraft that's not eligible for lookup now may become eligible as more messages are
                // received and its details are populated, so return a result with a "requeue" flag returned
                // by the eligibility assessor
                return new(false, eligible.Requeue);
            }

            // Lookup the aircraft
            var aircraft = await _aircraftApiWrapper.LookupAircraftAsync(address, "");
            if (aircraft == null)
            {
                // If the API can't find the aircraft, not only has this lookup failed but there's no point
                // retrying
                return new(false, false);
            }

            // Lookup the flight. The eligibility criteria mean we'll only get to this point if it *should* be
            // possible to get a successful response here
            var flight = await LookupFlightAsync(type, address, departureAirportCodes, arrivalAirportCodes);

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

            // If we reach here the API's done everything possible to get the details so there's no point
            // re-queueing the request
            return new(successful, false);
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
            => await _activeFlightApiWrapper.LookupFlightsInBoundingBox(centreLatitude, centreLongitude, rangeNm);

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

            // It's an active flight. If the API supports flight lookup by address, use it
            if (_activeFlightApiWrapper.SupportsLookupBy(ApiProperty.AircraftAddress))
            {
                flight = await _activeFlightApiWrapper.LookupFlightAsync(ApiProperty.AircraftAddress, address, address, departureAirportCodes, arrivalAirportCodes);
                return flight;
            }

            // Load the tracked aircraft record
            var aircraft = await _trackedAircraftWriter?.GetAsync(x => x.Address == address);
            if (aircraft == null)
            {
                // This is an error as the eligibility criteria shouldn't allow us to get this far if there's aircraft record
                _logger.LogMessage(Severity.Error, $"Unable to find tracked aircraft record for aicraft with address '{address}'");
                return null;
            }

            // Lookup the flight number based on the callsign and check it's found
                _logger.LogMessage(Severity.Debug, $"Looking up flight number for callsign '{aircraft.Callsign}'");
            var flightNumber = await _flightNumberApiWrapper.GetFlightNumberFromCallsignAsync(aircraft.Callsign);
            if (flightNumber == null)
            {
                _logger.LogMessage(Severity.Warning, $"No flight number mapping for callsign '{aircraft.Callsign}' : Unable to look up flight by number");
                return null;
            }

            // Lookup the flight by number
            _logger.LogMessage(Severity.Info, $"Looking up flight by number {flightNumber.Number} for aircraft {address}");
            flight = await _activeFlightApiWrapper.LookupFlightAsync(ApiProperty.FlightNumber, flightNumber.Number, address, departureAirportCodes, arrivalAirportCodes);
            return flight;
        }
    }
}