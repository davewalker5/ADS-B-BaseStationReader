using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
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
        private readonly ISightingManager _sightingManager;
        private readonly ITrackedAircraftWriter _trackedAircraftWriter;

        public ExternalApiWrapper(
            int maximumLookupAttempts,
            ITrackerLogger logger,
            AirlineManager airlineManager,
            IAircraftManager aircraftManager,
            IManufacturerManager manufacturerManager,
            IModelManager modelManager,
            IFlightManager flightManager,
            ISightingManager sightingManager,
            ITrackedAircraftWriter trackedAircraftWriter)
        {
            _maximumLookupAttempts = maximumLookupAttempts;
            _logger = logger;
            _register = new ExternalApiRegister(logger);
            _airlineApiWrapper = new AirlineApiWrapper(logger, _register, airlineManager);
            _activeFlightWrapper = new ActiveFlightApiWrapper(logger, _register, _airlineApiWrapper, flightManager);
            _historicalFlightWrapper = new HistoricalFlightApiWrapper(logger, _register, _airlineApiWrapper, flightManager, trackedAircraftWriter);
            _aircraftApiWrapper = new AircraftApiWrapper(logger, _register, aircraftManager, modelManager, manufacturerManager);
            _airportWeatherApiWrapper = new AirportWeatherApiWrapper(logger, _register);
            _sightingManager = sightingManager;
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
            // Check the maximum number of attempts hasn't been reached. A maximum of 0 indicates unlimited attempts
            if (_maximumLookupAttempts > 0)
            {
                // Look for a tracked aircraft with the specified address, no lookup timestamp and a number of attempts
                // less than the maximum
                var trackedAircraft = await _trackedAircraftWriter.GetAsync(x =>
                    (x.Address == address) &&
                    (x.LookupTimestamp == null) &&
                    (x.LookupAttempts < _maximumLookupAttempts));

                // If the result is NULL, either the aircraft isn't there at all, it's already been successfully looked
                // up or the maximum attempts have been reached
                if (trackedAircraft == null)
                {
                    _logger.LogMessage(Severity.Warning, $"Aircraft {address} is not tracked, has already been lookup up or has reached the maximum lookup attempts");
                    return false;
                }
            }
            
            // Lookup the flight
            var flight = type == ApiEndpointType.ActiveFlights ?
                await LookupActiveFlightAsync(address, departureAirportCodes, arrivalAirportCodes) :
                await LookupHistoricalFlightAsync(address, departureAirportCodes, arrivalAirportCodes);

            // Lookup the aircraft
            var aircraft = await LookupAircraftAsync(address, flight?.ModelICAO);

            // The lookup is considered successful if the aircraft and flight are valid
            var successful = (aircraft != null) && (flight != null);

            // Update the lookup properties on the tracked aircraft record
            await _trackedAircraftWriter.UpdateLookupProperties(address, successful, _maximumLookupAttempts);

            // If the lookup was successful and sighting creation is requested, save the relationship
            // between the flight and the aircraft as a sighting on this date
            if (createSighting && successful)
            {
                _ = await _sightingManager.AddAsync(aircraft.Id, flight.Id, DateTime.Today);
            }

            // The lookup was successful if both aircraft and flight were looked up successfully
            return successful;
        }

        /// <summary>
        /// Look up an active flight and store it locally
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirports"></param>
        /// <param name="arrivalAirports"></param>
        /// <returns></returns>
        public async Task<Flight> LookupActiveFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
            => await _activeFlightWrapper.LookupFlightAsync(address, departureAirportCodes, arrivalAirportCodes);

        /// <summary>
        /// Identify and save historical flight details for a tracked aircraft
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        public async Task<Flight> LookupHistoricalFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
            => await _historicalFlightWrapper.LookupFlightAsync(address, departureAirportCodes, arrivalAirportCodes);

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
        /// Look up an airline and save it locally
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Airline> LookupAirlineAsync(string icao, string iata, string name)
            => await _airlineApiWrapper.LookupAirlineAsync(icao, iata, name);

        /// <summary>
        /// Look up an aircraft and save it locally
        /// </summary>
        /// <param name="address"></param>
        /// <param name="alternateModelICAO"></param>
        /// <returns></returns>
        public async Task<Aircraft> LookupAircraftAsync(string address, string alternateModelICAO)
            => await _aircraftApiWrapper.LookupAircraftAsync(address, alternateModelICAO);

        /// <summary>
        /// Lookup the current weather for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupAirportWeather(string icao)
            => await _airportWeatherApiWrapper.LookupAirportWeather(icao);
    }
}