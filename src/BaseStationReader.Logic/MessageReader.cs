using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.Logic
{
    [ExcludeFromCodeCoverage]
    public class MessageReader : IMessageReader
    {
        private readonly string _server;
        private readonly int _port;

        public event EventHandler<MessageReadEventArgs>? MessageRead;

        public MessageReader(string server, int port)
        {
            _server = server;
            _port = port;
        }

        /// <summary>
        /// Start reading messages from the server and port specified in the constructor, notifying
        /// subscribers as each message is read
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task Start(CancellationToken token)
        {
            using (var client = new TcpClient(_server, _port))
            {
                NetworkStream stream = client.GetStream();
                using (var reader = new StreamReader(stream))
                {
                    while (!token.IsCancellationRequested)
                    {
                        string? message = await reader.ReadLineAsync(token);
                        if (message != null)
                        {
                            MessageRead?.Invoke(this, new MessageReadEventArgs { Message = message });
                        }
                    }
                }
            }
        }
    }
}