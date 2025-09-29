using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Interfaces.Api
{
    public interface IAircraftApi
    {
        Task<Dictionary<ApiProperty, string>> LookupAircraftAsync(string address);
    }
}
