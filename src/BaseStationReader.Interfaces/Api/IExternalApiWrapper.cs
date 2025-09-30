using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Interfaces.Api
{
    public interface IExternalApiWrapper
    {
        Task<Flight> LookupActiveFlightAsync(string address, IEnumerable<string> departureAirportCodes, IEnumerable<string> arrivalAirportCodes);
        Task<Flight> LookupHistoricalFlightAsync(string address, IEnumerable<string> departureAirportCodes, IEnumerable<string> arrivalAirportCodes);
        Task<List<Flight>> LookupActiveFlightsInBoundingBox(double centreLatitude, double centreLongitude, double rangeNm);
        Task<Airline> LookupAirlineAsync(string icao, string iata);
        Task<Aircraft> LookupAircraftAsync(string address, string alternateModelICAO);
        Task<IEnumerable<string>> LookupAirportWeather(string icao);

        Task<LookupResult> LookupAsync(
            ApiEndpointType type,
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes,
            bool createSighting);
    }
}