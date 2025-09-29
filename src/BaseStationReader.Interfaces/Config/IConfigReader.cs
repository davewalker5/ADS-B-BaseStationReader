namespace BaseStationReader.Interfaces.Config
{
    public interface IConfigReader<T> where T : class
    {
        T Read(string jsonFileName);
    }
}