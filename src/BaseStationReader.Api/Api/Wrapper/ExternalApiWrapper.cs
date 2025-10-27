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

        public ExternalApiWrapper(IDatabaseManagementFactory factory)
        {
            _factory = factory;
            _register = new ExternalApiRegister(factory.Logger);
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
            // Check the address matches the 24-bit ICAO address pattern
            if (!_addressRegex.IsMatch(request.AircraftAddress))
            {
                _factory.Logger.LogMessage(Severity.Warning, $"'{request.AircraftAddress}' is not a valid aircraft address");
                return new(false, false);
            }

            // Attempt to retrieve the tracked aircraft record for the specified address - the retrieval accounts for
            // excluded addresses and callsigns
            // TODO: This needs to be a "candidate result" or some such so it can relay information about requeue attempts
            var trackedAircraft = await _factory.TrackedAircraftWriter.GetLookupCandidateAsync(request.AircraftAddress);
            if (trackedAircraft == null)
            {
                _factory.Logger.LogMessage(Severity.Warning, $"'{request.AircraftAddress}' is not a candidate for lookup");
                return new(false, false);
            }

            return new(false, false);
        }

        /// <summary>
        /// Lookup the current weather for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao)
            => [];

        /// <summary>
        /// Lookup the weather forecast for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupAirportWeatherForecastAsync(string icao)
            => [];
    }
}