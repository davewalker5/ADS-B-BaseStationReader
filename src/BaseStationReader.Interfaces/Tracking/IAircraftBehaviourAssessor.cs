using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Tracking
{
    public interface IAircraftBehaviourAssessor
    {
        AircraftBehaviour Assess(TrackedAircraft aircraft);
    }
}