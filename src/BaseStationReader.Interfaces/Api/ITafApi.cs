namespace BaseStationReader.Interfaces.Api
{
    public interface ITafApi : IExternalApi
    {
        Task<IEnumerable<string>> LookupAirportWeatherForecastAsync(string icao);
    }
}