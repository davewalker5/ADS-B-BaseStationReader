using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal class SkyLinkAircraftApi : ExternalApiBase, IAircraftApi
    {
        public SkyLinkAircraftApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            ExternalApiSettings settings) : base(logger, client)
        {
        }

        public Task<Dictionary<ApiProperty, string>> LookupAircraftAsync(string address)
        {
            throw new NotImplementedException();
        }
    }
}