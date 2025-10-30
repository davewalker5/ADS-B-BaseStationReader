using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using BaseStationReader.Interfaces.Messages;

namespace BaseStationReader.BusinessLogic.Messages
{
    [ExcludeFromCodeCoverage]
    public class TrackerTcpClient : ITrackerTcpClient
    {
        private TcpClient _client;

        /// <summary>
        /// Connect to a host and port
        /// </summary>
        /// <param name="host"></param>
        /// <param name="port"></param>
        public void Connect(string host, int port)
            => _client = new TcpClient(host, port);

        /// <summary>
        /// Get the network stream for the connection
        /// </summary>
        /// <returns></returns>
        public Stream GetStream()
            =>  _client?.GetStream();

        /// <summary>
        /// Dispose the client
        /// </summary>
        public void Dispose()
            => _client?.Dispose();
    }
}