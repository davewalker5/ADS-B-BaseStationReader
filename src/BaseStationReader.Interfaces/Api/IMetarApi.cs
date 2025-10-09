namespace BaseStationReader.Interfaces.Api
{
    public interface IMetarApi : IExternalApi
    {
        Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao);
    }
}