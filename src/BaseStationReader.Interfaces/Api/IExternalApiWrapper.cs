using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Api
{
    public interface IExternalApiWrapper
    {
        Task<LookupResult> LookupAsync(ApiLookupRequest request);
        Task<List<Flight>> LookupActiveFlightsInBoundingBoxAsync(double centreLatitude, double centreLongitude, double rangeNm);
        Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao);
        Task<IEnumerable<string>> LookupAirportWeatherForecastAsync(string icao);
    }
}