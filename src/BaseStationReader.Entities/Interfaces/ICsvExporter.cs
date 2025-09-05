namespace BaseStationReader.Entities.Interfaces
{
    public interface ICsvExporter<in T> where T: class
    {
        void Export(IEnumerable<T> entities, string fileName, char separator);
    }
}