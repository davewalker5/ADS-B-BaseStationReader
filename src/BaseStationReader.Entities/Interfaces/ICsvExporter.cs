using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ICsvExporter<T> where T: class
    {
        void Export(IEnumerable<T> entities, string fileName, char separator);
    }
}