namespace BaseStationReader.Interfaces.Database
{
    public interface IDataCleaner
    {
        Task CleanAirlines();
        Task CleanManufacturers();
        Task CleanModels();
    }
}