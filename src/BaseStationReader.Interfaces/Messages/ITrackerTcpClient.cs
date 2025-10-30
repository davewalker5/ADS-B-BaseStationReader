namespace BaseStationReader.Interfaces.Messages
{
    public interface ITrackerTcpClient : IDisposable
    {
        void Connect(string host, int port);
        Stream GetStream();
    }
}