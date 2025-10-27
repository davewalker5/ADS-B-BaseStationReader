namespace BaseStationReader.Interfaces.Api
{
    public interface IWeatherLookupManager
    {
        Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao);
        Task<IEnumerable<string>> LookupAirportWeatherForecastAsync(string icao);
    }
}