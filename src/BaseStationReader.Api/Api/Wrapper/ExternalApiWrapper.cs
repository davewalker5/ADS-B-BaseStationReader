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
