using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Simulator
{
    public interface IMessageGeneratorWrapper : IMessageGenerator
    {
        Message Generate(TrackedAircraft aircraft, string generatorType);
    }
}