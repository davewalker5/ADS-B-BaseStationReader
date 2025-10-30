using BaseStationReader.BusinessLogic.Events;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Tracking
{
    [TestClass]
    public class MessageReaderNotificationSenderTest
    {
        private const int DelayMs = 100;
        private const string Message = "This Is A Message";

        private readonly ITrackerLogger _logger = new MockFileLogger();
        private List<string> _received = [];

        [TestInitialize]
        public void Initialise()
        {
            _received.Clear();
        }

        [TestMethod]
        public async Task SendMessageReadNotificationTestAsync()
        {
            var sender = new MessageReaderNotificationSender(_logger);
            sender.SendMessageReadNotification(this, OnMessageReadNotification, Message);
            await Task.Delay(DelayMs);
            Assert.HasCount(1, _received);
            Assert.AreEqual(Message, _received[0]);
        }

        private void OnMessageReadNotification(object sender, MessageReadEventArgs e)
        {
            _received.Add(e.Message);
            _logger.LogMessage(Severity.Info, e.Message);
        }
    }
}