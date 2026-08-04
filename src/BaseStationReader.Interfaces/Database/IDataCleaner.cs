namespace BaseStationReader.Interfaces.Database
{
    public interface IDataCleaner
    {
        Task CleanAirlinesAsync();
        Task CleanManufacturersAsync();
        Task CleanModelsAsync();
    }
}
