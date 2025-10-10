using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Api
{
    public interface IExternalApiWrapper
    {
        Task<FlightNumber> GetFlightNumberFromCallsignAsync(string callsign, DateTime? timestamp = null);
        Task<List<FlightNumber>> GetFlightNumbersFromCallsignsAsync(IEnumerable<string> callsigns, DateTime? timestamp = null);
        Task<List<FlightNumber>> GetFlightNumbersForTrackedAircraftAsync(IEnumerable<TrackingStatus> statuses);
        Task<LookupResult> LookupAsync(ApiLookupRequest request);
        Task<List<Flight>> LookupActiveFlightsInBoundingBoxAsync(double centreLatitude, double centreLongitude, double rangeNm);
        Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao);
        Task<IEnumerable<string>> LookupAirportWeatherForecastAsync(string icao);
    }
}