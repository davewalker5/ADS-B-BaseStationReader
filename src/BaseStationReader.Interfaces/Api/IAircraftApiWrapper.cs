using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IAircraftApiWrapper
    {
        Task<Aircraft> LookupAircraftAsync(string address, string alternateModelICAO);
    }
}