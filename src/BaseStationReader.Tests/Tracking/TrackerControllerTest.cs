using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.Tests.Entities;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Logging;
using System.Text;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Tests.Tracking
{
    [TestClass]
    public class TrackerControllerTest
    {
        private const int MessageReaderIntervalMs = 200;
        private const int TrackerRecentMs = 4 * MessageReaderIntervalMs;
        private const int TrackerStaleMs = TrackerRecentMs + 2 * MessageReaderIntervalMs;
        private const int TrackerRemovedMs = TrackerStaleMs + 2 * MessageReaderIntervalMs;
        private const int MaximumTestRunTimeMs = TrackerRemovedMs + 2 * MessageReaderIntervalMs;

        private readonly TrackerApplicationSettings _settings = new()
        {
            MaximumLookups = 5,
            TimeToLock = 900000,
            Host = "",
            Port = 0,
            SocketReadTimeout = MaximumTestRunTimeMs,
            ReceiverLatitude = 51.14810180664062,
            ReceiverLongitude = -0.19027799367905,
            EnableSqlWriter = true,
            AutoLookup = false,
            WriterBatchSize = 20000,
            WriterInterval = 30000,
            TrackedBehaviours = [.. Enum.GetValues<AircraftBehaviour>()],
            TrackPosition = true,
            TimeToRecent = TrackerRecentMs,
            TimeToStale = TrackerStaleMs,
            TimeToRemoval = TrackerRemovedMs,
            ClearDown = false
        };

        private ITrackerLogger _logger = new MockFileLogger();
        private ITrackerController _controller;

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
            var tcpClient = new MockTrackerTcpClient(buffer);

            // Construct the tracker controller itself
            var httpClient = new MockTrackerHttpClient();
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _controller = new TrackerController(_logger, context, null, httpClient, tcpClient, _settings, [], []);
        }

        [TestMethod]
        public async Task TestAircraftTracker()
        {
            // Wire up the event handlers
            _controller.AircraftAdded += OnAircraftNotification;
            _controller.AircraftUpdated += OnAircraftNotification;
            _controller.AircraftRemoved += OnAircraftNotification;

            try
            {
                var source = new CancellationTokenSource(MaximumTestRunTimeMs);
                await _controller.StartAsync(source.Token);
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
