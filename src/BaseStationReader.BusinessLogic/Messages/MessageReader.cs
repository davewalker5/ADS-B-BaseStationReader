using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using System.Diagnostics.CodeAnalysis;
using System.Net.Sockets;

namespace BaseStationReader.BusinessLogic.Messages
{
    [ExcludeFromCodeCoverage]
    public class MessageReader : IMessageReader
    {
        private readonly ITrackerLogger _logger;
        private readonly string _server;
        private readonly int _port;
        private readonly int _readTimeout;

        public event EventHandler<MessageReadEventArgs> MessageRead;

        public MessageReader(ITrackerLogger logger, string server, int port, int readTimeout)
        {
            _logger = logger;
            _server = server;
            _port = port;
            _readTimeout = readTimeout;
        }

        /// <summary>
        /// Start reading messages from the server and port specified in the constructor, notifying
        /// subscribers as each message is read
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken token)
        {
            // Continue until cancellation's requested
            while (!token.IsCancellationRequested)
            {
                // Get a TCP client used to read the message stream
                using (var client = new TcpClient(_server, _port))
                {
                    // Get a network stream and set the timeout
                    NetworkStream stream = client.GetStream();
                    stream.ReadTimeout = _readTimeout;

                    // Create a stream reader and begin reading messages
                    using (var reader = new StreamReader(stream))
                    {
                        // Wait for cancellation to be requested or for a read timeout
                        var timedOut = false;
                        while (!token.IsCancellationRequested && !timedOut)
                        {
                            try
                            {
                                // Read the next message and notify subscribers
                                string message = await reader.ReadLineAsync(token);
                                if (!string.IsNullOrEmpty(message))
                                {
                                    try
                                    {
                                        MessageRead?.Invoke(this, new MessageReadEventArgs { Message = message });
                                    }
                                    catch (Exception ex)
                                    {
                                        // Log and sink the exception. The reader has to be protected from errors in the
                                        // subscriber callbacks or the application will stop updating
                                        _logger.LogException(ex);
                                    }
                                }
                            }
                            catch (IOException)
                            {
                                // The read has timed out - set the flag that will break out of the loop
                                // and cause the application to reconnect and try again
                                timedOut = true;
                            }
                        }
                    }
                }
            }
        }
    }
}