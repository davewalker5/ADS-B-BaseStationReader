using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Api
{
    public interface IExternalApiWrapper
    {
        Task<FlightNumber> GetFlightNumberFromCallsignAsync(string callsign, DateTime? timestamp = null);
        Task<List<FlightNumber>> GetFlightNumbersFromCallsignsAsync(IEnumerable<string> callsigns, DateTime? timestamp = null);
        Task<List<FlightNumber>> GetFlightNumbersForTrackedAircraftAsync(IEnumerable<TrackingStatus> statuses);

        Task<LookupResult> LookupAsync(
            ApiEndpointType type,
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes,
            bool createSighting);

        Task<List<Flight>> LookupActiveFlightsInBoundingBoxAsync(double centreLatitude, double centreLongitude, double rangeNm);

        Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao);
        Task<IEnumerable<string>> LookupAirportWeatherForecastAsync(string icao);
    }
}