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

        public ExternalApiWrapper(IDatabaseManagementFactory factory)
        {
            _factory = factory;
            _register = new ExternalApiRegister(factory.Logger);
            var airlineLookupManager = new AirlineLookupManager(_register, factory);
            _aircraftLookupManager = new AircraftLookupManager(_register, factory);
            _flightLookupManager = new FlightLookupManager(_register, factory, airlineLookupManager);
            _weatherLookupManager = new WeatherLookupManager(factory.Logger, _register);
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
            var trackedAircraft = await _factory.TrackedAircraftWriter.GetLookupCandidateAsync(request.AircraftAddress);
            if (trackedAircraft == null)
            {
                // If the callsign is blank, the aircraft may become eligible for lookup if the callsign is subsequently
                // filled in, so allow requeues. Otherwise, the exclusion is more permanent so don't allow requeues
                _factory.Logger.LogMessage(Severity.Warning, $"'{request.AircraftAddress}' is not a candidate for lookup");
                var allowRequeue = string.IsNullOrEmpty(trackedAircraft.Callsign);
                return new(false, allowRequeue);
            }

            // Lookup the aircraft
            var aircraft = await _aircraftLookupManager.IdentifyAircraftAsync(request.AircraftAddress);
            if (aircraft == null)
            {
                // If an aircraft isn't identifiable, there's no point allowing requeues
                return new(false, false);
            }

            // Lookup the flight
            var flight = await _flightLookupManager.IdentifyFlightAsync(trackedAircraft, request.DepartureAirportCodes, request.ArrivalAirportCodes);
            if (flight == null)
            {
                return new(false, false);
            }

            // We have both an aircraft and a flight - if required, create a sighting
            if (request.CreateSighting)
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
    }
}