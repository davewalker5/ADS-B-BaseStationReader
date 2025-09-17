using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAirlinesApi
    {
        Task<Dictionary<ApiProperty, string>> LookupAirlineByIATACodeAsync(string iata);
        Task<Dictionary<ApiProperty, string>> LookupAirlineByICAOCodeAsync(string icao);
    }
}