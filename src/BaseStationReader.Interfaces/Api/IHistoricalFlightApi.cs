using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Interfaces.Api
{
    public interface IHistoricalFlightApi
    {
        Task<List<Dictionary<ApiProperty, string>>> LookupFlightsByAircraftAsync(string address);
    }
}