using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IAircraftApi : IExternalApi
    {
        Task<Dictionary<ApiProperty, string>> LookupAircraftAsync(string address);
    }
}