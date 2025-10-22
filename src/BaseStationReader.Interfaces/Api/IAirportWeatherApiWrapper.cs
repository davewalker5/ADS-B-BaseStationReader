namespace BaseStationReader.Interfaces.Api
{
    public interface IAirportWeatherApiWrapper : IExternalApi
    {
        Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao);
        Task<IEnumerable<string>> LookupAirportWeatherForecastAsync(string icao);
    }
}