using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Messages;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Tests.Mocks
{
    internal class MockMessageReader : IMessageReader
    {
        private readonly ITrackerLogger _logger;
        private readonly IEnumerable<string> _messages;
        private readonly int _delay;

        public event EventHandler<MessageReadEventArgs> MessageRead;

        public MockMessageReader(ITrackerLogger logger, IEnumerable<string> messages, int delay)
        {
            _logger = logger;
            _messages = messages;
            _delay = delay;
        }

        /// <summary>
        /// Process the message queue supplied in the constructor
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken token)
        {
            await Task.Run(() =>
            {
                // Iterate over the message collection passed to the constructor, sending a notification
                // for each one
                foreach (var message in _messages)
                {
                    // For the staleness measurement and removal to work, the date and time specified in the
                    // messages need to be change to "now", for both generated and logged timestamps

                    // Get the date and time string replacements in the expected format
                    var now = DateTime.Now;
                    var date = now.ToString("yyyy/MM/dd");
                    var time = now.ToString("HH:mm:ss.fff");

                    // Split the message and perform the replacements
                    var fields = message.Split(",");
                    fields[(int)MessageField.DateGenerated] = date;
                    fields[(int)MessageField.TimeGenerated] = time;
                    fields[(int)MessageField.DateLogged] = date;
                    fields[(int)MessageField.TimeLogged] = time;

                    // Reconstruct the message and notify subscribers
                    var reconstructed = string.Join(",", fields);
                    _logger.LogMessage(Severity.Info, $"Sending message: {reconstructed}");
                    MessageRead?.Invoke(this, new MessageReadEventArgs { Message = reconstructed });

                    // Allow some time for the (asynchronous) message propagation from the Aircraft Tracker
                    // to arrive
                    Thread.Sleep(_delay);
                }

                // If the message queue's been emptied, send empty messages at the defined interval. This is
                // needed because in the context of the tests the aircraft tracker test exits prematurely if
                // the reader stops sending messages
                // while (!token.IsCancellationRequested)
                // {
                //    MessageRead?.Invoke(this, new MessageReadEventArgs { Message = "" });
                // }

            }, token);
        }
    }
}
