using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftLookupManager
    {
        Task<AircraftDetails?> LookupAircraft(string address);
    }
}