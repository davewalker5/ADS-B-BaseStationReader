using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic
{
    public interface IAircraftLockManager
    {
        Task<TrackedAircraft> GetActiveAircraftAsync(string address);
    }
}