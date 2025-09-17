namespace BaseStationReader.Entities.Interfaces
{
    public interface IReceiverSimulator
    {
        Task StartAsync();
        void Stop();
    }
}