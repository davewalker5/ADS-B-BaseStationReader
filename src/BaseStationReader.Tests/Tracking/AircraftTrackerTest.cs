using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Messages;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Messages;
using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.Tests.Entities;
using BaseStationReader.Tests.Mocks;
using System.Diagnostics;
using BaseStationReader.BusinessLogic.Events;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Tests.Tracking
{
    // [TestClass]
    public class AircraftTrackerTest
    {
        private const int MessageReaderIntervalMs = 200;
        private const int TrackerRecentMs = 4 * MessageReaderIntervalMs;
        private const int TrackerStaleMs = TrackerRecentMs + 2 * MessageReaderIntervalMs;
        private const int TrackerRemovedMs = TrackerStaleMs + 2 * MessageReaderIntervalMs;
        private const int MaximumTestRunTimeMs = TrackerRemovedMs + 2 * MessageReaderIntervalMs;

        private ITrackerLogger _logger = new MockFileLogger();
        private List<AircraftNotificationData> _notifications = [];

        [TestMethod]
        public void TestAircraftTracker()
        {
            string[] messages = [
                "MSG,8,1,1,3965A3,1,2023/08/23,12:07:27.929,2023/08/23,12:07:28.005,,,,,,,,,,,,0",
                "MSG,6,1,1,3965A3,1,2023/08/23,12:07:27.932,2023/08/23,12:07:28.006,,,,,,,,6303,0,0,0,"
            ];

            // Create a mock reader and parser
            var reader = new MockMessageReader(_logger, messages, MessageReaderIntervalMs);
            var parsers = new Dictionary<MessageType, IMessageParser>
            {
                { MessageType.MSG, new MsgMessageParser() }
            };

            // Create an aircraft tracker and wire up the event handlers
            var timer = new MockTrackerTimer(TrackerRecentMs / 10.0);
            var assessor = new SimpleAircraftBehaviourAssessor();
            var updater = new AircraftPropertyUpdater(_logger, null, assessor);
            
            var notificationSender = new AircraftNotificationSender(
                _logger,
                Enum.GetValues<AircraftBehaviour>(),
                null,
                null,
                null,
                true);

            var tracker = new AircraftTracker(reader,
                parsers,
                timer,
                updater,
                notificationSender,
                [],
                [],
                TrackerRecentMs,
                TrackerStaleMs,
                TrackerRemovedMs);

            tracker.AircraftAdded += OnAircraftNotification;
            tracker.AircraftUpdated += OnAircraftNotification;
            tracker.AircraftRemoved += OnAircraftNotification;

            // Start a stopwatch, that's used to make sure the test doesn't run continuously if
            // something goes awry
            var stopwatch = Stopwatch.StartNew();
            stopwatch.Start();

            // Start the tracker and wait until all the messages have been sent or it's clear there's a problem
            tracker.Start();
            while (stopwatch.ElapsedMilliseconds < MaximumTestRunTimeMs)
            {
            }

            // Stop the tracker
            tracker.Stop();

            // Construct the expected de-duplicated sequence of notifications
            var expected = new List<AircraftNotificationType>
            {
                AircraftNotificationType.Added,
                AircraftNotificationType.Updated,
                AircraftNotificationType.Recent,
                AircraftNotificationType.Stale,
                AircraftNotificationType.Removed
            };

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
            foreach (var notification in duplicates)
            {
                _notifications.Remove(notification);
            }

            // The actual notifications list should now be equal to the length of the expected list
            Assert.HasCount(expected.Count, _notifications);

            // Now confirm the notifications we do have arrived in the right order with the correct aircraft data
            for (int i = 0; i < expected.Count; i++)
            {
                // Confirm the notification type is correct
                Assert.AreEqual(expected[i], _notifications[i].NotificationType);

                // Confirm the aircraft details are correct. The first copy won't have a squawk code,
                // the remainder will
                var expectedSquawk = (expected[i] == AircraftNotificationType.Added) ? null : "6303";
                ConfirmAircraftProperties(_notifications[i].Aircraft, expectedSquawk);
            }
        }

        /// <summary>
        /// Confirm the aircraft properties match the expected values
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="expectedSquawk"></param>
        private static void ConfirmAircraftProperties(TrackedAircraft aircraft, string expectedSquawk)
        {
            Assert.AreEqual("3965A3", aircraft.Address);
            Assert.IsNull(aircraft.Callsign);
            Assert.IsNull(aircraft.Altitude);
            Assert.IsNull(aircraft.GroundSpeed);
            Assert.IsNull(aircraft.Track);
            Assert.IsNull(aircraft.Latitude);
            Assert.IsNull(aircraft.Longitude);
            Assert.IsNull(aircraft.VerticalRate);
            if (expectedSquawk != null)
            {
                Assert.AreEqual(expectedSquawk, aircraft.Squawk);
            }
            else
            {
                Assert.IsNull(aircraft.Squawk);
            }
        }

        private void OnAircraftNotification(object sender, AircraftNotificationEventArgs e)
        {
            lock (_notifications)
            {
                _logger.LogMessage(Severity.Info, $"Received {e.NotificationType} notification");
                _notifications.Add(new AircraftNotificationData
                {
                    Aircraft = (TrackedAircraft)e.Aircraft.Clone(),
                    NotificationType = e.NotificationType
                });
            }
        }
    }
}
