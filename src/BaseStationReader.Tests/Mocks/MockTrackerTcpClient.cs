using BaseStationReader.Interfaces.Messages;

namespace BaseStationReader.Tests.Mocks
{
    internal class MockTrackerTcpClient : MemoryStream, ITrackerTcpClient
    {
        public MockTrackerTcpClient(byte[] buffer) : base(buffer) { }

        public void Connect(string host, int port)
        {
        }

        public Stream GetStream()
            => this;
    }
}