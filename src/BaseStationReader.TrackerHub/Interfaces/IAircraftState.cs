using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.TrackerHub.Interfaces
{
    public interface IAircraftState
    {
        IReadOnlyCollection<TrackedAircraft> All();
        void Upsert(TrackedAircraft a);
        void Remove(string icao, DateTimeOffset whenUtc);
    }
}