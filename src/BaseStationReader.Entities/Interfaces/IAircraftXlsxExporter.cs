using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftXlsxExporter
    {
        void Export(IEnumerable<Aircraft> aircraft, string fileName);
    }
}
