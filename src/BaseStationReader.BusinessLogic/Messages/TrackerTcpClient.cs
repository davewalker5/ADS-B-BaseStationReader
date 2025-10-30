using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using BaseStationReader.Interfaces.Messages;

namespace BaseStationReader.BusinessLogic.Messages
{
    [ExcludeFromCodeCoverage]
    public class TrackerTcpClient : ITrackerTcpClient
    {
        private TcpClient _client;
        private NetworkStream _stream;
        private StreamReader _reader;

        /// <summary>
        /// Connect to a host and port
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void Connect(string host, int port, int readTimeoutMs)
        {
            _client = new TcpClient(host, port);
            _stream = _client.GetStream();
            _stream.ReadTimeout = readTimeoutMs;
            _reader = new StreamReader(_stream);
        }

        /// <summary>
        /// Read the next line from the network stream
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task<string> ReadLineAsync(CancellationToken token)
            => await _reader.ReadLineAsync(token);

        /// <summary>
        /// Dispose the client
        /// </summary>
        public void Dispose()
        {
            _reader?.Dispose();
            _stream?.Dispose();
            _client?.Dispose();
        }
    }
}