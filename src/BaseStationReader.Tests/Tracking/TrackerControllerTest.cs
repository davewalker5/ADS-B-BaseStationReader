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
        private const int MaximumTestRunTimeMs = TrackerRemovedMs + 5 * MessageReaderIntervalMs;

        private readonly TrackerApplicationSettings _settings = new()
        {
            TimeToLock = 900000,
            Host = "",
            Port = 0,
            SocketReadTimeout = MaximumTestRunTimeMs,
            ReceiverLatitude = 51.14810180664062,
            ReceiverLongitude = -0.19027799367905,
            ReceiverElevation = 120,
            DefaultProfileName = "Test Default Profile",
            EnableSqlWriter = true,
            TrackingLogSummaryInterval = 100,
            TrackedBehaviours = [.. Enum.GetValues<AircraftBehaviour>()],
            TrackPosition = true,
            TimeToRecent = TrackerRecentMs,
            TimeToStale = TrackerStaleMs,
            TimeToRemoval = TrackerRemovedMs
        };

        private MockFileLogger _logger = new();
        private ITrackerController _controller;
        private BaseStationReaderDbContext _context;

        private List<AircraftNotificationData> _notifications = [];


        [TestInitialize]
        public void Initialise()
        {
            // Define the test messages
            string[] messages = [
                "SEL,,496,,,",
                "MSG,8,1,1,3965A3,1,2023/08/23,12:07:27.929,2023/08/23,12:07:28.005,,,,,,,,,,,,0",
                "MSG,6,1,1,3965A3,1,2023/08/23,12:07:27.932,2023/08/23,12:07:28.006,,,,,,,,6303,0,0,0,"
            ];

            // Construct the message reader
            var buffer = Encoding.UTF8.GetBytes(string.Join("\n", messages) + "\n");
            var tcpClient = new MockTrackerTcpClient(buffer, holdOpenAfterEnd: true);

            // Construct the tracker controller itself
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _controller = new TrackerController(_logger, _context, tcpClient, _settings,
                sessionNotes: "Clear skies; testing session context.",
                spoolQueue: new TemporarySpoolQueue());
        }

        [TestMethod]
        [TestCategory("TimingSensitive")]
        public async Task TestAircraftTracker()
        {
            // Wire up the event handlers
            _controller.AircraftEvent += OnAircraftNotification;
            var rawMessageCount = 0;
            _controller.MessageReceived += (_, _) => Interlocked.Increment(ref rawMessageCount);

            try
            {
                var source = new CancellationTokenSource(MaximumTestRunTimeMs);
                await _controller.StartAsync(source.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected when the token is cancelled
            }

            // De-duplicate the notifications
            _notifications = _notifications.GroupBy(p => p.NotificationType).Select(g => g.First()).ToList();

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
            Assert.AreEqual(3, rawMessageCount,
                "The raw heartbeat must include records that do not produce an aircraft notification.");

            // Now confirm all the expected notifications are there
            foreach (var notificationType in expected)
            {
                Assert.HasCount(1, _notifications.Where(x => x.NotificationType == notificationType));
            }

            // A removed aircraft must not survive in the authoritative snapshot consumed by the Hub UI.
            Assert.IsEmpty(_controller.State);

            // The run creates one immutable profile snapshot and links every persisted observation to it.
            var session = _context.ObservationSessions.Single();
            Assert.AreEqual("Test Default Profile", session.ProfileName);
            Assert.AreEqual(session.StartedAtUtc.ToString("yyyy-MM-dd HH:mm:ss"), session.Name);
            Assert.AreEqual("Clear skies; testing session context.", session.Notes);
            Assert.AreEqual(_settings.Host, session.Host);
            Assert.AreEqual(_settings.Port, session.Port);
            Assert.AreEqual(120, session.ReceiverElevation);
            Assert.IsNull(session.MinimumAltitude);
            Assert.IsNull(session.MaximumAltitude);
            Assert.IsNull(session.MaximumDistance);
            Assert.AreEqual(string.Join(",", _settings.TrackedBehaviours), session.IncludedBehaviours);
            Assert.AreEqual(DateTimeKind.Utc, session.StartedAtUtc.Kind);
            Assert.IsTrue(_context.TrackedAircraft.All(x => x.SessionId == session.Id));

            Assert.IsTrue(_logger.Messages.Any(x =>
                x.Severity == Severity.Info && x.Message.StartsWith("Tracking summary:")));
            Assert.IsTrue(_logger.Messages.Any(x =>
                x.Severity == Severity.Info && x.Message.StartsWith("Tracking final summary:")));
            Assert.IsTrue(_logger.Messages.Any(x =>
                x.Severity == Severity.Info && x.Message.StartsWith("Aircraft changes:")));
        }

        [TestMethod]
        public async Task PersistsDetectedAircraftWithoutQualifyingPositionsTest()
        {
            // Exclude every behaviour from position tracking while retaining the receiver observation itself.
            _settings.TrackedBehaviours = [];
            using var source = new CancellationTokenSource(MessageReaderIntervalMs * 3);

            try
            {
                await _controller.StartAsync(source.Token);
            }
            catch (TaskCanceledException)
            {
                // Expected when the bounded test session ends.
            }

            var session = _context.ObservationSessions.Single();
            var aircraft = _context.TrackedAircraft.Single();
            Assert.AreEqual(session.Id, aircraft.SessionId);
            Assert.AreEqual("3965A3", aircraft.Address);
            Assert.IsEmpty(_context.Positions);
        }

        private void OnAircraftNotification(object sender, AircraftNotificationEventArgs e)
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
