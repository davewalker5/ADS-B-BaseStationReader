namespace BaseStationReader.Interfaces.Api
{
    public interface IMetarApi
    {
        Task<IEnumerable<string>> LookupAirportWeather(string icao);
    }
}