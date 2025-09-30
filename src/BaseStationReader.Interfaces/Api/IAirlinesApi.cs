using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IAirlinesApi : IExternalApi
    {
        Task<Dictionary<ApiProperty, string>> LookupAirlineByIATACodeAsync(string iata);
        Task<Dictionary<ApiProperty, string>> LookupAirlineByICAOCodeAsync(string iata);
    }
}