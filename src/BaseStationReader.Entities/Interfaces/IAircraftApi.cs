using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftApi
    {
        Task<Dictionary<ApiProperty, string>> LookupAircraftAsync(string address);
    }
}
