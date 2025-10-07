using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Api
{
    public interface IExternalApiWrapper
    {
        Task<FlightNumber> GetFlightNumberFromCallsignAsync(string callsign, DateTime? timestamp = null);
        Task<List<FlightNumber>> GetFlightNumbersFromCallsigns(IEnumerable<string> callsigns, DateTime? timestamp = null);
        Task<List<FlightNumber>> GetFlightNumbersForTrackedAircraftAsync(IEnumerable<TrackingStatus> statuses);

        Task<bool> LookupAsync(
            ApiEndpointType type,
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes,
            bool createSighting);

        Task<List<Flight>> LookupActiveFlightsInBoundingBox(double centreLatitude, double centreLongitude, double rangeNm);

        Task<IEnumerable<string>> LookupCurrentAirportWeather(string icao);
        Task<IEnumerable<string>> LookupAirportWeatherForecast(string icao);
    }
}