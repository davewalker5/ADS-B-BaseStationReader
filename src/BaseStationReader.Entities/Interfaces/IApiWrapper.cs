using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IApiWrapper
    {
        Task<Flight> LookupActiveFlightAsync(string address);
        Task<Airline> LookupAirlineAsync(string icao, string iata);
        Task<Aircraft> LookupAircraftAsync(string address);
        Task<Flight> LookupAndStoreFlightAsync(string address);
        Task<Aircraft> LookupAndStoreAircraftAsync(string address);
        Task<Airline> LookupAndStoreAirlineAsync(string icao, string iata);
    }
}