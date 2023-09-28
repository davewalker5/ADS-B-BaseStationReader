using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IActiveFlightApi
    {
        Task<Dictionary<ApiProperty, string>?> LookupFlightByAircraft(string address);
    }
}