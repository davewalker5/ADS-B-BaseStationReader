using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftBehaviourAssessor
    {
        AircraftBehaviour Assess(Aircraft aircraft);
    }
}