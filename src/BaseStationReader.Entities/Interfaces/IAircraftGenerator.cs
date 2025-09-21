using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftGenerator
    {
        TrackedAircraft Generate(IEnumerable<string> existingAddresses);
    }
}