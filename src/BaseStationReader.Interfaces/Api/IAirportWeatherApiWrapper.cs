namespace BaseStationReader.Interfaces.Api
{
    public interface IAirportWeatherApiWrapper
    {
        Task<IEnumerable<string>> LookupCurrentAirportWeather(string icao);
        Task<IEnumerable<string>> LookupAirportWeatherForecast(string icao);
    }
}