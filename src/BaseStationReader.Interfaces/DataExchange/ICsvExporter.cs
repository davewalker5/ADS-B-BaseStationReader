namespace BaseStationReader.Interfaces.DataExchange
{
    public interface ICsvExporter<in T> where T: class
    {
        void Export(IEnumerable<T> entities, string fileName, char separator);
    }
}