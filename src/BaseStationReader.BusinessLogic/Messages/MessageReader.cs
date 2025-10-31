using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Messages;
using BaseStationReader.Interfaces.Logging;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Events;

namespace BaseStationReader.BusinessLogic.Messages
{
    [ExcludeFromCodeCoverage]
    public class MessageReader : IMessageReader
    {
        private readonly ITrackerTcpClient _client;
        private readonly ITrackerLogger _logger;
        private readonly IMessageReaderNotificationSender _sender;
        private readonly string _server;
        private readonly int _port;
        private readonly int _readTimeout;
        private bool _timedOut = false;

        public event EventHandler<MessageReadEventArgs> MessageRead;

        public MessageReader(
            ITrackerTcpClient client,
            ITrackerLogger logger,
            IMessageReaderNotificationSender sender,
            string server,
            int port,
            int readTimeout)
        {
            _client = client;
            _logger = logger;
            _sender = sender;
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
            // Connect to the server
            _client.Connect(_server, _port, _readTimeout);

            // Enter the message reader loop
            while (!token.IsCancellationRequested && !_timedOut)
            {
                try
                {
                    // Read the next message
                    var message = await _client.ReadLineAsync(token).ConfigureAwait(false);
                    if (!string.IsNullOrEmpty(message))
                    {
                        try
                        {
                            // Notify subscribers
                            _sender.SendMessageReadNotification(this, MessageRead, message);
                        }
                        catch (Exception ex)
                        {
                            _logger.LogMessage(Severity.Error, ex.Message);
                            _logger.LogException(ex);
                        }
                    }
                }
                catch (IOException ex)
                {
                    // The read has timed out - set the flag that will break out of the read loop and cause the
                    // application to reconnect and try again
                    _logger.LogMessage(Severity.Error, $"Message reading has timed out");
                    _logger.LogMessage(Severity.Error, ex.Message);
                    _logger.LogException(ex);
                    _timedOut = true;
                }
            }
        }
    }
}