using System.Text.RegularExpressions;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Api.Wrapper
{
    internal class ExternalApiWrapper : IExternalApiWrapper
    {
        private static readonly Regex _addressRegex = new(@"^[A-Za-z0-9]{6}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly IExternalApiRegister _register;
        private readonly IDatabaseManagementFactory _factory;
        private readonly IAircraftLookupManager _aircraftLookupManager;
        private readonly IFlightLookupManager _flightLookupManager;
        private readonly IWeatherLookupManager _weatherLookupManager;
        private readonly IScheduleLookupManager _scheduleLookupManager;

        public ExternalApiWrapper(IDatabaseManagementFactory factory)
        {
            _factory = factory;
            _register = new ExternalApiRegister(factory.Logger);
            _aircraftLookupManager = new AircraftLookupManager(_register, factory);
            _flightLookupManager = new FlightLookupManager(_register, factory);
            _weatherLookupManager = new WeatherLookupManager(factory.Logger, _register);
            _scheduleLookupManager = new ScheduleLookupManager(factory.Logger, _register);
        }

        /// <summary>
        /// Register an external API instance
        /// </summary>
        /// <param name="type"></param>
        /// <param name="api"></param>
        public void RegisterExternalApi(ApiEndpointType type, IExternalApi api)
            => _register.RegisterExternalApi(type, api);

        /// <summary>
        /// Resolves an aircraft from local data or the registered aircraft API.
        /// </summary>
        /// <param name="address">The aircraft's six-character ICAO address.</param>
        /// <returns>The resolved aircraft, or null when it cannot be identified.</returns>
        public async Task<Aircraft> LookupAircraftAsync(string address)
            => await _aircraftLookupManager.IdentifyAircraftAsync(address);

        /// <summary>
        /// Resolves a flight from local data or the registered flights API.
        /// </summary>
        /// <param name="address">The aircraft's six-character ICAO address.</param>
        /// <param name="callsign">The observed flight callsign.</param>
        /// <returns>The resolved flight, or null when it cannot be identified.</returns>
        public async Task<Flight> LookupFlightAsync(string address, string callsign)
        {
            address = address?.Trim().ToUpperInvariant() ?? string.Empty;
            callsign = callsign?.Trim().ToUpperInvariant() ?? string.Empty;

            // Interactive lookups are independent of tracked rows and use the current time for flight matching.
            var trackedAircraft = new TrackedAircraft
            {
                Address = address,
                Callsign = callsign,
                LastSeen = DateTime.Now,
                Status = TrackingStatus.Active
            };

            // Try local callsign mappings first, even when an aircraft address is available.
            var flight = await _flightLookupManager.IdentifyFlightAsync(trackedAircraft, null, null, false);
            if (flight != null)
            {
                return flight;
            }

            // Configured flight APIs search by aircraft address. For callsign-only requests, use the most
            // recently tracked local aircraft with that callsign before falling back to the API.
            if (string.IsNullOrWhiteSpace(address))
            {
                var observedAircraft = await _factory.TrackedAircraftWriter.GetAsync(x => x.Callsign == callsign);
                if (observedAircraft != null)
                {
                    trackedAircraft.Address = observedAircraft.Address;
                    trackedAircraft.LastSeen = observedAircraft.LastSeen;
                }
            }

            return string.IsNullOrWhiteSpace(trackedAircraft.Address)
                ? null
                : await _flightLookupManager.IdentifyFlightAsync(trackedAircraft, null, null, true);
        }

        /// <summary>
        /// Lookup a flight and aircraft given a 24-bit aircraft ICAO address and filtering parameters
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<LookupResult> LookupAsync(ApiLookupRequest request)
        {
            var departureAirports = request.DepartureAirportCodes != null ? string.Join(", ", request.DepartureAirportCodes) : "";
            var arrivalAirports = request.ArrivalAirportCodes != null ? string.Join(", ", request.ArrivalAirportCodes) : "";

            _factory.Logger.LogMessage(Severity.Info,
                $"Attempting lookup: " +
                $"Aircraft Address = {request.AircraftAddress}, " +
                $"Departure Airports = {departureAirports}, " +
                $"Arrival Airports = {arrivalAirports}, " +
                $"Create Sighting = {request.CreateSighting}");

            // Check the address matches the 24-bit ICAO address pattern
            if (!_addressRegex.IsMatch(request.AircraftAddress))
            {
                _factory.Logger.LogMessage(Severity.Warning, $"'{request.AircraftAddress}' is not a valid aircraft address");
                return new(false, false);
            }

            // See if the aircraft is a valid candidate for lookup - the retrieval accounts for exclusions
            var trackedAircraft = await _factory.TrackedAircraftWriter.GetSightingCreationCandidateAsync(request.AircraftAddress);
            if (trackedAircraft == null)
            {
                // As we didn't find a tracked aircraft record, there's no point attempting to update the lookup properties
                // but requeues are allowed in case the tracked aircraft record hasn't been written yet
                _factory.Logger.LogMessage(Severity.Warning, $"'{request.AircraftAddress}' is not a candidate for lookup");
                return new(false, true);
            }

            // Lookup the aircraft
            var aircraft = await _aircraftLookupManager.IdentifyAircraftAsync(
                request.AircraftAddress,
                request.AllowExternalApiLookup);
            if (aircraft == null)
            {
                return new(false, false);
            }

            // Lookup the flight
            var flight = await _flightLookupManager.IdentifyFlightAsync(
                trackedAircraft,
                request.DepartureAirportCodes,
                request.ArrivalAirportCodes,
                request.AllowExternalApiLookup);
            if (flight == null)
            {
                // If the callsign is blank, the aircraft may become eligible for lookup if the callsign is subsequently
                // filled in, so allow requeues. Otherwise, the exclusion is more permanent so don't allow requeues
                var allowRequeue = string.IsNullOrEmpty(trackedAircraft?.Callsign);
                return new(false, allowRequeue);
            }

            // We have both an aircraft and a flight - if required, create a sighting
            if (request.CreateSighting && aircraft.Id > 0 && flight.Id > 0)
            {
                _ = await _factory.SightingManager.AddAsync(aircraft.Id, flight.Id, trackedAircraft.FirstSeen);
            }

            return new(true, false);
        }

        /// <summary>
        /// Lookup the current weather for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao)
            => await _weatherLookupManager.LookupCurrentAirportWeatherAsync(icao);

        /// <summary>
        /// Lookup the weather forecast for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupAirportWeatherForecastAsync(string icao)
            => await _weatherLookupManager.LookupAirportWeatherForecastAsync(icao);

        /// <summary>
        /// Retrieves an airport schedule and extracts its flight IATA code mappings.
        /// </summary>
        /// <param name="iata">The schedule airport IATA code.</param>
        /// <param name="from">The beginning of the schedule window.</param>
        /// <param name="to">The end of the schedule window.</param>
        /// <returns>The extracted flight mappings.</returns>
        public async Task<List<FlightScheduleEntry>> LookupSchedulesAsync(string iata, DateTime from, DateTime to)
            => await _scheduleLookupManager.LookupSchedulesAsync(iata, from, to);
    }
}
