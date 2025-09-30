using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IHistoricalFlightsApi : IExternalApi
    {
        Task<List<Dictionary<ApiProperty, string>>> LookupFlightsByAircraftAsync(string address);
    }
}