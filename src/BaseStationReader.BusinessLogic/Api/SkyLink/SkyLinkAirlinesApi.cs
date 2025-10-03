using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal class SkyLinkAirlinesApi : ExternalApiBase, IAirlinesApi
    {
        public SkyLinkAirlinesApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            ExternalApiSettings settings) : base(logger, client)
        {
        }

        public Task<Dictionary<ApiProperty, string>> LookupAirlineByIATACodeAsync(string iata)
        {
            throw new NotImplementedException();
        }

        public Task<Dictionary<ApiProperty, string>> LookupAirlineByICAOCodeAsync(string iata)
        {
            throw new NotImplementedException();
        }
    }
}