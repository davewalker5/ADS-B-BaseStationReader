using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Api.Wrapper
{
    internal class ScheduleLookupManager : IScheduleLookupManager
    {
        private readonly ITrackerLogger _logger;
        private readonly IExternalApiRegister _register;

        /// <summary>
        /// Initialises a schedule lookup manager using the external API register.
        /// </summary>
        /// <param name="logger">The application logger.</param>
        /// <param name="register">The register containing configured API implementations.</param>
        public ScheduleLookupManager(ITrackerLogger logger, IExternalApiRegister register)
        {
            _logger = logger;
            _register = register;
        }

        /// <summary>
        /// Retrieves an airport schedule and extracts its flight IATA code mappings.
        /// </summary>
        /// <param name="iata">The schedule airport IATA code.</param>
        /// <param name="from">The beginning of the schedule window.</param>
        /// <param name="to">The end of the schedule window.</param>
        /// <returns>The extracted mappings, or null when no schedules API is registered.</returns>
        public async Task<List<FlightIATACodeMapping>> LookupSchedulesAsync(string iata, DateTime from, DateTime to)
        {
            // Both retrieval and extraction must use the same schedules API implementation.
            if (_register.GetInstance(ApiEndpointType.Schedules) is not ISchedulesApi api) return null;

            _logger.LogMessage(Severity.Info, $"Looking up schedules for airport IATA = '{iata}'");
            var schedules = await api.LookupSchedulesRawAsync(iata, from, to);
            var mappings = api.ExtractFlightMapping(schedules, iata);
            _logger.LogMessage(Severity.Info, $"Extracted {mappings.Count} schedule mappings for {iata}");
            return mappings;
        }
    }
}
