using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Interfaces.Api
{
    public interface IExternalApiWrapper
    {
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