using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic
{
    public interface IAircraftLockManager
    {
        Task<Aircraft> GetActiveAircraft(string address);
    }
}