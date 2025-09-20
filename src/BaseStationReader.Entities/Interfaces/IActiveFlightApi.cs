using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IActiveFlightApi
    {
        Task<Dictionary<ApiProperty, string>> LookupFlightByAircraftAsync(string address);
    }
}