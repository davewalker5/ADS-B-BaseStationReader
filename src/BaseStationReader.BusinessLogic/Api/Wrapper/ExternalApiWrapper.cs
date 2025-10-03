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
            // Check the aircraft is eligible for lookup
            var eligible = await IsEligibleForLookup(address);
            if (!eligible)
            {
                return false;
            }

            // Lookup the flight
            var flight = type == ApiEndpointType.ActiveFlights ?
                await _activeFlightWrapper.LookupFlightAsync(address, departureAirportCodes, arrivalAirportCodes) :
                await _historicalFlightWrapper.LookupFlightAsync(address, departureAirportCodes, arrivalAirportCodes);

            // Lookup the aircraft
            var aircraft = await _aircraftApiWrapper.LookupAircraftAsync(address, flight?.ModelICAO);

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
        /// Check that an aircraft is eligible for lookup
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private async Task<bool> IsEligibleForLookup(string address)
        {
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