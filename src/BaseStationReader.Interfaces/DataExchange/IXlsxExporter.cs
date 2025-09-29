namespace BaseStationReader.Interfaces.DataExchange
{
    public interface IXlsxExporter<in T> where T : class
    {
        void Export(IEnumerable<T> entities, string fileName, string worksheetName);
    }
}
