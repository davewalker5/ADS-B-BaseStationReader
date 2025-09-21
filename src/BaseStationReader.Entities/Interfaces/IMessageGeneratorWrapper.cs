using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IMessageGeneratorWrapper : IMessageGenerator
    {
        Message Generate(TrackedAircraft aircraft, string generatorType);
    }
}