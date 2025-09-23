using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IApiWrapper
    {
        Task LookupAsync(
            string address,
            IEnumerable<string> departureAirports,
            IEnumerable<string> arrivalAirports,
            bool createSighting);

        Task<Flight> LookupFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes);

        Task<Airline> LookupAirlineAsync(string icao, string iata);
        Task<Aircraft> LookupAircraftAsync(string address, string alternateModelICAO);

        Task<Flight> LookupAndStoreFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes);

        Task<Aircraft> LookupAndStoreAircraftAsync(string address, string alternateModelICAO);
        Task<Airline> LookupAndStoreAirlineAsync(string icao, string iata);
    }
}