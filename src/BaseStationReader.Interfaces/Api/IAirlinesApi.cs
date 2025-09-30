using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IAirlinesApi
    {
        Task<Dictionary<ApiProperty, string>> LookupAirlineByIATACodeAsync(string iata);
        Task<Dictionary<ApiProperty, string>> LookupAirlineByICAOCodeAsync(string iata);
    }
}