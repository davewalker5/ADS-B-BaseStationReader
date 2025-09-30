using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Simulator
{
    public interface IMessageGenerator
    {
        Message Generate(TrackedAircraft aircraft);
    }
}