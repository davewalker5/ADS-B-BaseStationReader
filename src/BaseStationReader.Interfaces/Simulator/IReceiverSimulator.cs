namespace BaseStationReader.Interfaces.Simulator
{
    public interface IReceiverSimulator
    {
        Task StartAsync();
        void Stop();
    }
}