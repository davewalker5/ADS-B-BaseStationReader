using System.Text;
using BaseStationReader.BusinessLogic.Events;
using BaseStationReader.BusinessLogic.Messages;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Messages;
using BaseStationReader.Tests.Mocks;


namespace BaseStationReader.Tests.Messages
{
    [TestClass]
    public class MessageReaderTest
    {
        private const int TokenLifespanMs = 2000;
        private readonly string[] _messages = [
            "MSG,8,1,1,3965A3,1,2023/08/23,12:07:27.929,2023/08/23,12:07:28.005,,,,,,,,,,,,0",
            "MSG,6,1,1,3965A3,1,2023/08/23,12:07:27.932,2023/08/23,12:07:28.006,,,,,,,,6303,0,0,0,",
            "MSG,7,1,1,407DCD,1,2023/08/23,12:12:35.113,2023/08/23,12:12:35.191,,18025,,,,,,,,,,"
        ];

        private ITrackerLogger _logger;
        private ITrackerTcpClient _client;
        private readonly List<string> _received = [];
        private byte[] _buffer;

        [TestInitialize]
        public void Initialise()
        {
            _logger = new MockFileLogger();
            _buffer = Encoding.UTF8.GetBytes(string.Join("\n", _messages) + "\n");
            _client = new MockTrackerTcpClient(_buffer);
            _received.Clear();
        }

        [TestMethod]
        public async Task MockNetworkStreamTestAsync()
        {
            var source = new CancellationTokenSource(250);
            var stream = new MockNetworkStream(_buffer);

            string line;
            try
            {
                do
                {
                    line = await stream.ReadLineAsync(source.Token);
                    if (!string.IsNullOrEmpty(line))
                    {
                        _received.Add(line);
                        _logger.LogMessage(Severity.Info, line);
                    }
                }
                while (true);
            }
            catch (TaskCanceledException)
            {
                // Expected exception when the token expires
            }

            AssertExpectedMessagesReceived();
        }

        [TestMethod]
        public async Task MockTrackerTcpClientTestAsync()
        {
            var source = new CancellationTokenSource(250);

            try
            {
                string line;
                do
                {
                    line = await _client.ReadLineAsync(source.Token);
                    if (!string.IsNullOrEmpty(line))
                    {
                        _received.Add(line);
                        _logger.LogMessage(Severity.Info, line);
                    }
                }
                while (true);
            }
            catch (TaskCanceledException)
            {
                // Expected exception when the token expires
            }

            AssertExpectedMessagesReceived();
        }

        [TestMethod]
        public async Task TestMessageReaderAsync()
        {
            var source = new CancellationTokenSource(TokenLifespanMs);
            var sender = new MessageReaderNotificationSender(_logger);
            var reader = new MessageReader(_client, _logger, sender, "", 0, 100);
            reader.MessageRead += OnMessageRead;

            try
            {
                await reader.StartAsync(source.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected exception when the token expires
            }

            reader.MessageRead -= OnMessageRead;

            Assert.HasCount(3, _received);
        }

        private void AssertExpectedMessagesReceived()
        {
            Assert.HasCount(3, _received);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(_messages[i], _received[i]);
            }
        }

        private void OnMessageRead(object sender, MessageReadEventArgs e)
        {
            _received.Add(e.Message);
            _logger.LogMessage(Severity.Info, e.Message);
        }
    }
}