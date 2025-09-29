using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Database
{
    public interface IAircraftLockManager
    {
        Task<TrackedAircraft> GetActiveAircraftAsync(string address);
    }
}