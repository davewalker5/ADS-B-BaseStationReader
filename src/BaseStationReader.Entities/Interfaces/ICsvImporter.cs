using CsvHelper.Configuration;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ICsvImporter<M, T>
        where M : ClassMap
        where T : class
    {
        List<T> Read(string filePath);
    }
}