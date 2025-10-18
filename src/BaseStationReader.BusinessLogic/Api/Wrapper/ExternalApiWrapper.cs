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
        private readonly ILookupEligibilityAssessor _lookupEligibilityAssessor;

        public ExternalApiWrapper(
            bool ignoreTrackingStatus,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory)
        {
            _logger = logger;
            _factory = factory;
            _register = new ExternalApiRegister(logger);
            _airlineApiWrapper = new AirlineApiWrapper(_register, factory);
            _activeFlightApiWrapper = new ActiveFlightApiWrapper(_register, _airlineApiWrapper, factory);
            _historicalFlightWrapper = new HistoricalFlightApiWrapper(_register, _airlineApiWrapper, factory);
            _aircraftApiWrapper = new AircraftApiWrapper(_register, factory);
            _airportWeatherApiWrapper = new AirportWeatherApiWrapper(logger, _register);
            _lookupEligibilityAssessor = new LookupEligibilityAssessor(_historicalFlightWrapper, _activeFlightApiWrapper, factory, ignoreTrackingStatus);
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
                // If we can't find the aircraft locally or via the APU, not only has this lookup failed but
                // there's no point retrying
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
            // Determine whether we're doing an active or historical flight lookup and get the
            // appropriate API instance
            IFlightApiWrapper api = request.FlightEndpointType == ApiEndpointType.ActiveFlights ?
                _activeFlightApiWrapper : _historicalFlightWrapper;

            // Preferentially use the callsign-to-flight mapping data to create the airline and flight
            (var number, var flight) = await CreateFlightFromMapping(request, api);

            // If that didn't work, fall back to using the API
            if (flight == null)
            {
                // Determine API capabilitues
                var canLookupByAddress = api.SupportsLookupBy(ApiProperty.AircraftAddress);
                var canLookupByNumber = api.SupportsLookupBy(ApiProperty.FlightIATA);

                if (canLookupByAddress)
                {
                    // Lookup the flight using the aircraft address
                    _logger.LogMessage(Severity.Info, $"Using the API to look up flight for aircraft {request.AircraftAddress} by address");
                    request.FlightPropertyType = ApiProperty.AircraftAddress;
                    request.FlightPropertyValue = request.AircraftAddress;
                    flight = await api.LookupFlightAsync(request);
                }
                else if (canLookupByNumber)
                {
                    // Lookup the flight using the flight IATA code
                    _logger.LogMessage(Severity.Info, $"Using the API to look up flight {number.FlightIATA} for aircraft {request.AircraftAddress} by IATA code");
                    request.FlightPropertyType = ApiProperty.FlightIATA;
                    request.FlightPropertyValue = number.FlightIATA;
                    flight = await api.LookupFlightAsync(request);
                }
                else
                {
                    _logger.LogMessage(Severity.Error, $"API does not support the available lookup criteria");
                }
            }

            return flight;
        }

        /// <summary>
        /// Create a flight from callsign-to-flight mapping data and return the mapping and flight instances
        /// </summary>
        /// <param name="request"></param>
        /// <param name="api"></param>
        /// <returns></returns>
        private async Task<(FlightIATACodeMapping, Flight)> CreateFlightFromMapping(ApiLookupRequest request, IFlightApiWrapper api)
        {
            // Lookup the tracked aircraft record
            _logger.LogMessage(Severity.Debug, $"Looking up tracked aircraft record for address '{request.AircraftAddress}'");
            var aircraft = await _factory.TrackedAircraftWriter?.GetAsync(x => x.Address == request.AircraftAddress);
            if (aircraft == null)
            {
                _logger.LogMessage(Severity.Warning, $"Unable to find tracked aircraft record for aicraft with address '{request.AircraftAddress}'");
                return (null, null);
            }

            // Look up the mapping based on the callsign
            _logger.LogMessage(Severity.Debug, $"Looking up mapping for callsign '{aircraft.Callsign}'");
            var number = await _factory.FlightIATACodeMappingManager.GetAsync(x => x.Callsign == aircraft.Callsign);
            if (string.IsNullOrEmpty(number?.FlightIATA))
            {
                _logger.LogMessage(Severity.Warning, $"No flight IATA code mapping for callsign '{aircraft.Callsign}'");
                return (null, null);
            }

            // Create the flight and airline from the mapping data, provided the airports aren't filtered out
            var flight = await api.CreateFlightFromMapping(request, number);
            return (number, flight);
        }
    }
}