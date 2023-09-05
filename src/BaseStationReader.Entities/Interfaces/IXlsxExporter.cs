using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IXlsxExporter<T> where T : class
    {
        void Export(IEnumerable<T> entities, string fileName, string worksheetName);
    }
}
