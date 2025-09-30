using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IHistoricalFlightsApi
    {
        Task<List<Dictionary<ApiProperty, string>>> LookupFlightsByAircraftAsync(string address);
    }
}