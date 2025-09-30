using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Interfaces.Api
{
    public interface ITrackerHttpClient
    {
        void SetRateLimits(ApiServiceType type, int limit);
        Task<HttpResponseMessage> SendAsync(ITrackerLogger logger, ApiServiceType type, HttpRequestMessage request);
    }
}