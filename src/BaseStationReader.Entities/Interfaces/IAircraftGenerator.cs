using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftGenerator
    {
        Aircraft Generate(IEnumerable<string> existingAddresses);
    }
}