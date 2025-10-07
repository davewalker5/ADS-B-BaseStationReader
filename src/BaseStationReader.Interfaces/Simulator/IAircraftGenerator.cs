using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Simulator
{
    public interface IAircraftGenerator
    {
        TrackedAircraft Generate(IEnumerable<string> existingAddresses);
    }
}