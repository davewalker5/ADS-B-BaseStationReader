namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerHttpClient
    {
        Task<HttpResponseMessage> GetAsync(string uri);
    }
}