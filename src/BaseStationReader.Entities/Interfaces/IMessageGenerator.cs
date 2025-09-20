using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IMessageGenerator
    {
        Message Generate(TrackedAircraft aircraft);
    }
}