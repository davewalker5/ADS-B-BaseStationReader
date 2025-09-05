using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Logic
{
    public interface IAircraftLockManager
    {
        Task<Aircraft> GetActiveAircraft(string address);
    }
}