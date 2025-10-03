using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal class SkyLinkActiveFlightApi : ExternalApiBase, IActiveFlightsApi
    {
        public SkyLinkActiveFlightApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            ExternalApiSettings settings) : base(logger, client)
        {
        }

        public Task<Dictionary<ApiProperty, string>> LookupFlightByAircraftAsync(string address)
        {
            throw new NotImplementedException();
        }

        public Task<List<Dictionary<ApiProperty, string>>> LookupFlightsInBoundingBox(double centreLatitude, double centreLongitude, double rangeNm)
        {
            throw new NotImplementedException();
        }
    }
}