namespace BaseStationReader.Interfaces.Api
{
    public interface IAirportWeatherApiWrapper
    {
        Task<IEnumerable<string>> LookupAirportWeather(string icao);
    }
}