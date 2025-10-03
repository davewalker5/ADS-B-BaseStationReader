using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal class SkyLinkMetarApi : ExternalApiBase, IMetarApi
    {
        public SkyLinkMetarApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            ExternalApiSettings settings) : base(logger, client)
        {
        }

        public Task<IEnumerable<string>> LookupAirportWeather(string icao)
        {
            throw new NotImplementedException();
        }
    }
}