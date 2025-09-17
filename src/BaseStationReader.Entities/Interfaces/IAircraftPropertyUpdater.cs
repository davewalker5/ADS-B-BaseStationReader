using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftPropertyUpdater
    {
        void UpdateProperties(Aircraft aircraft, Message msg);
        void UpdateBehaviour(Aircraft aircraft, decimal? lastAltitude);
    }
}