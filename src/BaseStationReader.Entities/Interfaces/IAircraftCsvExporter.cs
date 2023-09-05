using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftCsvExporter
    {
        void Export(IEnumerable<Aircraft> aircraft, string fileName, char separator);
    }
}