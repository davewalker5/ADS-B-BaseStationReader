using BaseStationReader.Entities.Lookup;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftApi
    {
        Task<Dictionary<ApiProperty, string>?> LookupAircraft(string address);
    }
}
