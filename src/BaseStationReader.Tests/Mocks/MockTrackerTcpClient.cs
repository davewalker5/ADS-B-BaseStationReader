using BaseStationReader.Interfaces.Messages;

namespace BaseStationReader.Tests.Mocks
{
    internal class MockTrackerTcpClient : ITrackerTcpClient
    {
        private readonly MockNetworkStream _stream;

        public MockTrackerTcpClient(byte[] buffer)
        {
            _stream = new MockNetworkStream(buffer);
        }

        /// <summary>
        /// Mock connection to a server and port
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void Connect(string host, int port, int readTimeout)
        {
        }

        /// <summary>
        /// Read the next line from the stream
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<string> ReadLineAsync(CancellationToken token)
            => await _stream.ReadLineAsync(token);

        /// <summary>
        /// IDisposable implementation
        /// </summary>
        public void Dispose()
        {
        }
    }
}