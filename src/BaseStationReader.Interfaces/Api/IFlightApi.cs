using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IFlightApi : IExternalApi
    {
        Task<List<Dictionary<ApiProperty, string>>> LookupFlightsAsync(string address, DateTime timestamp);
    }
}