using BaseStationReader.BusinessLogic.Events;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Events;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Tracking
{
    [TestClass]
    public class QueuedWriterNotificationSenderTest
    {
        private const int DelayMs = 100;

        private readonly Random _random = new();
        private ITrackerLogger _logger;
        private IQueuedWriterNotificationSender _sender;

        private List<long> _received = [];

        [TestInitialize]
        public void Initialise()
        {
            _logger = new MockFileLogger();
            _sender = new QueuedWriterNotificationSender(_logger);
            _received.Clear();
        }

        [TestMethod]
        public async Task SendBatchStartedNotificationTest()
        {
            var initialQueueSize = _random.Next(3000, 5000);
            _sender.SendBatchStartedNotification(this, OnBatchStartedNotification, initialQueueSize);
            await Task.Delay(DelayMs);
            Assert.HasCount(1, _received);
            Assert.AreEqual(initialQueueSize, _received[0]);
        }

        [TestMethod]
        public async Task SendBatchCompletedNotificationTest()
        {
            var initialQueueSize = _random.Next(3000, 5000);
            var processed = _random.Next(100, 1000);
            var finalQueueSize = initialQueueSize - processed;
            var duration = _random.Next(4000, 10000);

            _sender.SendBatchCompletedNotification(this, OnBatchCompletedNotification, initialQueueSize, finalQueueSize, processed, duration);
            await Task.Delay(DelayMs);

            Assert.HasCount(4, _received);
            Assert.AreEqual(initialQueueSize, _received[0]);
            Assert.AreEqual(processed, _received[1]);
            Assert.AreEqual(finalQueueSize, _received[2]);
            Assert.AreEqual(duration, _received[3]);
        }

        private void OnBatchStartedNotification(object sender, BatchStartedEventArgs e)
        {
            _received.Add(e.QueueSize);
            _logger.LogMessage(Severity.Info, $"Batch Started, Queue Size = {e.QueueSize}");
        }

        private void OnBatchCompletedNotification(object sender, BatchCompletedEventArgs e)
        {
            _received.Add(e.InitialQueueSize);
            _received.Add(e.EntriesProcessed);
            _received.Add(e.FinalQueueSize);
            _received.Add(e.Duration);

            _logger.LogMessage(
                Severity.Info,
                $"Batch Completed, " +
                $"Processed = {e.EntriesProcessed}, " +
                $"Initial Queue Size = {e.InitialQueueSize}, " +
                $"Final Queue Size = {e.FinalQueueSize}, " +
                $"Duration = {e.Duration}");
        }
    }
}