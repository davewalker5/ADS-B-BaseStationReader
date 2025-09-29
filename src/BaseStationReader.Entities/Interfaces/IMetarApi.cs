namespace BaseStationReader.Entities.Interfaces
{
    public interface IMetarApi
    {
        Task<IEnumerable<string>> LookupAirportWeather(string icao);
    }
}