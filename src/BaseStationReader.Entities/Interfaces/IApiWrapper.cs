using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IApiWrapper
    {
        Task<Flight> LookupFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes);

        Task<Airline> LookupAirlineAsync(string icao, string iata);
        Task<Aircraft> LookupAircraftAsync(string address);

        Task<Flight> LookupAndStoreFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes);

        Task<Aircraft> LookupAndStoreAircraftAsync(string address);
        Task<Airline> LookupAndStoreAirlineAsync(string icao, string iata);
    }
}