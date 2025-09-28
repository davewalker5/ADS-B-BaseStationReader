namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerHttpClient
    {
        Task<HttpResponseMessage> SendAsync(HttpRequestMessage request);
    }
}