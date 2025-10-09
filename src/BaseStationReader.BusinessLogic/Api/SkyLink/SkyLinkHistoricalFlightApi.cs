using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
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
            ExternalApiSettings settings) : base(ApiEndpointType.ActiveFlights, logger, client, factory, settings)
        {
        }

        public Task<List<Dictionary<ApiProperty, string>>> LookupFlightsByAircraftAsync(string address, DateTime date)
        {
            throw new NotImplementedException();
        }
    }
}