using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Api.SkyLink
{
    internal class SkyLinkHistoricalFlightApi : SkyLinkFlightApiBase, IHistoricalFlightsApi
    {
        public SkyLinkHistoricalFlightApi(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(ApiEndpointType.HistoricalFlights, client, factory, settings)
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
                Factory.Logger.LogMessage(Severity.Error, $"Aircraft with address {address} is not tracked");
                return null;
            }

            // Check it has a callsign
            if (string.IsNullOrEmpty(aircraft.Callsign))
            {
                Factory.Logger.LogMessage(Severity.Error, $"Aircraft with address {address} has no callsign");
                return null;
            }

            // Get the flight IATA code from the callsign mappings
            var mapping = await Factory.FlightIATACodeMappingManager.GetAsync(x => x.Callsign == aircraft.Callsign);
            if (mapping == null)
            {
                Factory.Logger.LogMessage(Severity.Error, $"Callsign {aircraft.Callsign} has no flight IATA code mapping");
                return null;
            }

            // Lookup the flight by flight IATA code
            var properties = await LookupFlightByNumberAsync(ApiEndpointType.HistoricalFlights, mapping.FlightIATA);
            return properties != null ? [properties] : null;
        }
    }
}