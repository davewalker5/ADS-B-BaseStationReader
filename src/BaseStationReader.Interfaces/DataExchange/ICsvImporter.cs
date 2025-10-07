using CsvHelper.Configuration;

namespace BaseStationReader.Interfaces.DataExchange
{
    public interface ICsvImporter<M, T>
        where M : ClassMap
        where T : class
    {
        List<T> Read(string filePath);
        Task Save(IEnumerable<T> entities);
        Task Import(string filePath);
    }
}