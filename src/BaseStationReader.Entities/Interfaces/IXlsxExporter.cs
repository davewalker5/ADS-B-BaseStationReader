namespace BaseStationReader.Entities.Interfaces
{
    public interface IXlsxExporter<in T> where T : class
    {
        void Export(IEnumerable<T> entities, string fileName, string worksheetName);
    }
}
