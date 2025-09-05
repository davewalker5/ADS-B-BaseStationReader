using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAirlinesApi
    {
        Task<Dictionary<ApiProperty, string>> LookupAirlineByIATACode(string iata);
        Task<Dictionary<ApiProperty, string>> LookupAirlineByICAOCode(string icao);
    }
}