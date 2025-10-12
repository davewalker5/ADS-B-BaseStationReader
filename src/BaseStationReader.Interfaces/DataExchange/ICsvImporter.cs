using CsvHelper.Configuration;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface ICsvImporter<M, T>
        where M : ClassMap
        where T : class
    {
        List<T> Read(string filePath);
        Task SaveAsync(IEnumerable<T> entities);
        Task ImportAsync(string filePath);
    }
}