using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    internal class ExternalApiWrapper : IExternalApiWrapper
    {
        private readonly ITrackerLogger _logger;
        private readonly IExternalApiRegister _register;
        private readonly IActiveFlightApiWrapper _activeFlightApiWrapper;
        private readonly IHistoricalFlightApiWrapper _historicalFlightWrapper;
        private readonly IAirlineApiWrapper _airlineApiWrapper;
        private readonly IAircraftApiWrapper _aircraftApiWrapper;
        private readonly IAirportWeatherApiWrapper _airportWeatherApiWrapper;
        private readonly IDatabaseManagementFactory _factory;
        private readonly IFlightNumberApiWrapper _flightNumberApiWrapper;
        private readonly ILookupEligibilityAssessor _lookupEligibilityAssessor;

        public ExternalApiWrapper(
            bool ignoreTrackingStatus,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory)
        {
            _logger = logger;
            _factory = factory;
            _register = new ExternalApiRegister(logger);
            _airlineApiWrapper = new AirlineApiWrapper(logger, _register, factory);
            _activeFlightApiWrapper = new ActiveFlightApiWrapper(logger, _register, _airlineApiWrapper, factory);
            _historicalFlightWrapper = new HistoricalFlightApiWrapper(logger, _register, _airlineApiWrapper, factory);
            _aircraftApiWrapper = new AircraftApiWrapper(logger, _register, factory);
            _airportWeatherApiWrapper = new AirportWeatherApiWrapper(logger, _register);
            _flightNumberApiWrapper = new FlightNumberApiWrapper(_logger, factory);
            _lookupEligibilityAssessor = new LookupEligibilityAssessor(logger, _historicalFlightWrapper, _activeFlightApiWrapper, factory, ignoreTrackingStatus);
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
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<LookupResult> LookupAsync(ApiLookupRequest request)
        {
            _logger.LogMessage(Severity.Info, $"Performing aircraft lookup : " +
                $"Flight API = {request.FlightEndpointType}, " +
                $"Address = {request.AircraftAddress}, " +
                $"Create Sighting = {request.CreateSighting}");

            // Check the aircraft is eligible for lookup
            var eligible = await _lookupEligibilityAssessor.IsEligibleForLookupAsync(request.FlightEndpointType, request.AircraftAddress);
            if (!eligible.Eligible)
            {
                // An aircraft that's not eligible for lookup now may become eligible as more messages are
                // received and its details are populated, so return a result with a "requeue" flag returned
                // by the eligibility assessor
                return new(false, eligible.Requeue);
            }

            // Lookup the aircraft
            var aircraft = await _aircraftApiWrapper.LookupAircraftAsync(request.AircraftAddress, "");
            if (aircraft == null)
            {
                // If the API can't find the aircraft, not only has this lookup failed but there's no point
                // retrying
                return new(false, false);
            }

            // Lookup the flight. The eligibility criteria mean we'll only get to this point if it *should* be
            // possible to get a successful response here
            var flight = await LookupFlightAsync(request);
            var haveFlight = flight != null;

            // Update the lookup properties on the tracked aircraft record
            var trackedAircraft = await _factory.TrackedAircraftWriter.UpdateLookupPropertiesAsync(request.AircraftAddress, haveFlight);

            // If the lookup was successful and sighting creation is requested, save the relationship
            // between the flight and the aircraft as a sighting on this date
            if (request.CreateSighting && haveFlight)
            {
                // Determine the sighting date. This is either the last seen date on the associated tracked
                // aircraft record or "today"
                var sightingDate = trackedAircraft != null ? trackedAircraft.LastSeen : DateTime.Today;
        
                _logger.LogMessage(Severity.Debug, $"Creating sighting for aircraft {aircraft.Id}, flight {flight.IATA}");
                _ = await _factory.SightingManager.AddAsync(aircraft.Id, flight.Id, sightingDate);
            }

            // If we reach here the API's done everything possible to get the details so there's no point
            // re-queueing the request
            return new(haveFlight, false);
        }

        /// <summary>
        /// Lookup the current weather for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao)
            => await _airportWeatherApiWrapper.LookupCurrentAirportWeatherAsync(icao);

        /// <summary>
        /// Lookup the weather forecast for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupAirportWeatherForecastAsync(string icao)
            => await _airportWeatherApiWrapper.LookupAirportWeatherForecastAsync(icao);

        /// <summary>
        /// Lookup a flight, detecting the right API instance and key for flight lookup
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        private async Task<Flight> LookupFlightAsync(ApiLookupRequest request)
        {
            Flight flight = null;

            // Determine whether we're doing an active or historical flight lookup and get the
            // appropriate API instance
            IFlightApiWrapper api = request.FlightEndpointType == ApiEndpointType.ActiveFlights ?
                _activeFlightApiWrapper : _historicalFlightWrapper;

            // If the API supports lookup by address, use that
            var lookupByAddressIsSupported = api.SupportsLookupBy(ApiProperty.AircraftAddress);
            if (lookupByAddressIsSupported)
            {
                // Lookup the flight using the aircraft address
                request.FlightPropertyType = ApiProperty.AircraftAddress;
                request.FlightPropertyValue = request.AircraftAddress;
                flight = await api.LookupFlightAsync(request);

                // If we got a flight, return it
                if (flight != null)
                {
                    return flight;
                }
            }

            // Load the tracked aircraft record. Fall through from the address-based lookup, above, to allow the
            // flight to be created from callsign mapping data, if possible
            var aircraft = await _factory.TrackedAircraftWriter?.GetAsync(x => x.Address == request.AircraftAddress);
            if (aircraft == null)
            {
                // This is an error as the eligibility criteria shouldn't allow us to get this far if there's aircraft record
                _logger.LogMessage(Severity.Error, $"Unable to find tracked aircraft record for aicraft with address '{request.AircraftAddress}'");
                return null;
            }

            // Lookup the flight number based on the callsign and check it's found
            _logger.LogMessage(Severity.Debug, $"Looking up flight number for callsign '{aircraft.Callsign}'");
            var number = await _flightNumberApiWrapper.GetFlightNumberFromCallsignAsync(aircraft.Callsign);
            if (string.IsNullOrEmpty(number.FlightIATA))
            {
                _logger.LogMessage(Severity.Warning, $"No flight number mapping for callsign '{aircraft.Callsign}' : Unable to look up flight by number");
                return null;
            }

            // If lookup by address is supported by the API, we've fallen through from above so don't attempt lookup
            // by flight number. If lookup by address is *not* supported, do make the attempt
            if (!lookupByAddressIsSupported)
            {
                // Lookup the flight by number
                request.FlightPropertyType = ApiProperty.FlightNumber;
                request.FlightPropertyValue = number.FlightIATA;

                _logger.LogMessage(Severity.Info, $"Looking up flight by number {number.FlightIATA} for aircraft {request.AircraftAddress}");
                flight = await api.LookupFlightAsync(request);
            }

            // If the flight still hasn't been found, use the flight number mapping data to create the airline and flight
            // provided the airports aren't not filtered out
            flight ??= await api.CreateFlightFromMapping(request, number);
            return flight;
        }
    }
}