using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftLookupManager
    {
        Task<Aircraft> LookupAircraftAsync(string address);
        Task<Flight> LookupActiveFlightAsync(string address);
    }
}