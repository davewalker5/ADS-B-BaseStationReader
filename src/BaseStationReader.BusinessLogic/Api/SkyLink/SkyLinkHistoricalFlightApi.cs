using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal class SkyLinkHistoricalFlightApi : SkyLinkFlightApiBase, IHistoricalFlightsApi
    {
        public SkyLinkHistoricalFlightApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(ApiEndpointType.HistoricalFlights, logger, client, factory, settings)
        {
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="address"></param>
        /// <param name="date"></param>
        /// <returns></returns>
        public async Task<List<Dictionary<ApiProperty, string>>> LookupFlightsByAircraftAsync(string address, DateTime date)
        {
            // Look up the tracked aircraft record
            var aircraft = await Factory.TrackedAircraftWriter.GetAsync(x => x.Address == address);
            if (aircraft == null)
            {
                Logger.LogMessage(Severity.Error, $"Aircraft with address {address} is not tracked");
                return null;
            }

            // Check it has a callsign
            if (string.IsNullOrEmpty(aircraft.Callsign))
            {
                Logger.LogMessage(Severity.Error, $"Aircraft with address {address} has no callsign");
                return null;
            }

            // Get the flight number from the callsign mappings
            var mapping = await Factory.FlightNumberMappingManager.GetAsync(x => x.Callsign == aircraft.Callsign);
            if (mapping == null)
            {
                Logger.LogMessage(Severity.Error, $"Callsign {aircraft.Callsign} has no flight number mapping");
                return null;
            }

            // Lookup the flight by flight number
            var properties = await LookupFlightByNumberAsync(mapping.FlightIATA);
            return properties != null ? [properties] : null;
        }
    }
}