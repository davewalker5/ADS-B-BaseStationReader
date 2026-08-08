using BaseStationReader.Interfaces.Messages;

namespace BaseStationReader.Tests.Mocks
{
    internal class MockTrackerTcpClient : ITrackerTcpClient
    {
        private readonly MockNetworkStream _stream;
        private readonly bool _holdOpenAfterEnd;

        public MockTrackerTcpClient(byte[] buffer, bool holdOpenAfterEnd = false)
        {
            _stream = new MockNetworkStream(buffer);
            _holdOpenAfterEnd = holdOpenAfterEnd;
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
        {
            var message = await _stream.ReadLineAsync(token);
            if (message is null && _holdOpenAfterEnd)
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, token);
            }

            return message;
        }

        /// <summary>
        /// IDisposable implementation
        /// </summary>
        public void Dispose()
        {
        }
    }
}
