using BaseStationReader.Entities.Config;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerHttpClient
    {
        void SetRateLimits(ApiServiceType type, int limit);
        Task<HttpResponseMessage> SendAsync(ITrackerLogger logger, ApiServiceType type, HttpRequestMessage request);
    }
}