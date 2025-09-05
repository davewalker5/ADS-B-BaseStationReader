namespace BaseStationReader.Entities.Interfaces
{
    public interface IReceiverSimulator
    {
        Task Start();
        void Stop();
    }
}