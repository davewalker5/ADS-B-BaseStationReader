using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftPropertyUpdater
    {
        void UpdateProperties(TrackedAircraft aircraft, Message msg);
        void UpdateBehaviour(TrackedAircraft aircraft, decimal? lastAltitude);
    }
}