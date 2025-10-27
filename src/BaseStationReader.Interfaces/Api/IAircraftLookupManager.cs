using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IAircraftLookupManager
    {
        Task<Aircraft> IdentifyAircraftAsync(string address);
    }
}