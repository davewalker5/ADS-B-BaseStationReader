using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftLookupManager
    {
        Task<AircraftDetails> LookupAircraftAsync(string address);
        Task<FlightDetails> LookupActiveFlightAsync(string address);
    }
}