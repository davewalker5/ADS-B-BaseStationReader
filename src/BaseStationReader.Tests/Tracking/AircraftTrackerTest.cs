using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Messages;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Messages;
using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.Tests.Entities;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.BusinessLogic.Events;
using BaseStationReader.Interfaces.Logging;
using System.Text;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Tests.Tracking
{
    [TestClass]
    public class AircraftTrackerTest
    {
        private const int MessageReaderIntervalMs = 200;
        private const int TrackerRecentMs = 4 * MessageReaderIntervalMs;
        private const int TrackerStaleMs = TrackerRecentMs + 2 * MessageReaderIntervalMs;
        private const int TrackerRemovedMs = TrackerStaleMs + 2 * MessageReaderIntervalMs;
        private const int MaximumTestRunTimeMs = TrackerRemovedMs + 2 * MessageReaderIntervalMs;

        private ITrackerLogger _logger = new MockFileLogger();
        private IAircraftTracker _tracker;

        private List<AircraftNotificationData> _notifications = [];

        [TestInitialize]
        public void Initialise()
        {
            // Define the test messages
            string[] messages = [
                "MSG,8,1,1,3965A3,1,2023/08/23,12:07:27.929,2023/08/23,12:07:28.005,,,,,,,,,,,,0",
                "MSG,6,1,1,3965A3,1,2023/08/23,12:07:27.932,2023/08/23,12:07:28.006,,,,,,,,6303,0,0,0,"
            ];

            // Construct the message reader
            var buffer = Encoding.UTF8.GetBytes(string.Join("\n", messages) + "\n");
            var client = new MockTrackerTcpClient(buffer);
            var readerSender = new MessageReaderNotificationSender(_logger);
            var reader = new MessageReader(client, _logger, readerSender, "", 0, 100);

            // Construct the message parsers
            var parsers = new Dictionary<MessageType, IMessageParser>
            {
                { MessageType.MSG, new MsgMessageParser() }
            };

            // Construct the helper classes for the aircraft tracker
            var assessor = new SimpleAircraftBehaviourAssessor();
            var updater = new AircraftPropertyUpdater(_logger, null, assessor);
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var trackerSender = new AircraftNotificationSender(_logger, behaviours, null, null, null, true);

            // Construct the aircraft tracker itself
            _tracker = new AircraftTracker(reader, parsers, updater, trackerSender, [], [], TrackerRecentMs, TrackerStaleMs, TrackerRemovedMs);
        }

        [TestMethod]
        public async Task TestAircraftTracker()
        {
            // Wire up the event handlers
            _tracker.AircraftAdded += OnAircraftNotification;
            _tracker.AircraftUpdated += OnAircraftNotification;
            _tracker.AircraftRemoved += OnAircraftNotification;

            try
            {
                var source = new CancellationTokenSource(MaximumTestRunTimeMs);
                await _tracker.StartAsync(source.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected when the token is cancelled
            }

            // Identify duplicates in the notifications list (for the Recent and Stale notification types)
            var duplicates = new List<AircraftNotificationData>();
            var previous = AircraftNotificationType.Unknown;
            foreach (var notification in _notifications)
            {
                if (notification.NotificationType == previous)
                {
                    duplicates.Add(notification);
                }

                previous = notification.NotificationType;
            }

            // Remove the duplicates
            _notifications.RemoveAll(x => duplicates.Contains(x));

            // Log the notifications - this provides useful information if there's a problem
            foreach (var notification in _notifications)
            {
                _logger.LogMessage(Severity.Info, $"{notification.NotificationType}: {notification.Aircraft}");
            }
            
            // Construct the expected de-duplicated sequence of notifications
            var expected = new List<AircraftNotificationType>
            {
                AircraftNotificationType.Added,
                AircraftNotificationType.Updated,
                AircraftNotificationType.Recent,
                AircraftNotificationType.Stale,
                AircraftNotificationType.Removed
            };

            // The actual notifications list should now be equal to the length of the expected list
            Assert.HasCount(expected.Count, _notifications);

            // Now confirm all the expected notifications are there
            foreach (var notificationType in expected)
            {
                Assert.HasCount(1, _notifications.Where(x => x.NotificationType == notificationType));
            }
        }

        private void OnAircraftNotification(object sender, AircraftNotificationEventArgs e)
        {
            _notifications.Add(new AircraftNotificationData
            {
                Aircraft = (TrackedAircraft)e.Aircraft.Clone(),
                NotificationType = e.NotificationType
            });
        }
    }
}
