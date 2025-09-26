using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IHistoricalFlightApi
    {
        Task<List<Dictionary<ApiProperty, string>>> LookupFlightsByAircraftAsync(string address);
    }
}