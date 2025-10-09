namespace BaseStationReader.Interfaces.Api
{
    public interface IAirportWeatherApiWrapper
    {
        Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao);
        Task<IEnumerable<string>> LookupAirportWeatherForecastAsync(string icao);
    }
}