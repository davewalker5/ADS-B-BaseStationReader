using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftLookupManager
    {
        Task<Flight> LookupActiveFlightAsync(string address);
        Task<Airline> LookupAirlineAsync(string icao, string iata);
        Task<Aircraft> LookupAircraftAsync(string address);
    }
}