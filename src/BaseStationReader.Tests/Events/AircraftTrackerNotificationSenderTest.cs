using BaseStationReader.BusinessLogic.Events;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Tests.Entities;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Tracking
{
    [TestClass]
    public class AircraftTrackerNotificationSenderTest
    {
        private const int DelayMs = 100;
        private const string Address = "485876";
        private const decimal Altitude = 29000M;
        private const decimal Latitude = 51.68825M;
        private const decimal Longitude = 51.68825M;
        private const double Distance = 24;

        private readonly ITrackerLogger _logger = new MockFileLogger();
        private List<AircraftNotificationData> _notifications = [];
        private TrackedAircraft _aircraft;
        private AircraftPosition _position;

        [TestInitialize]
        public void Initialise()
        {
            _notifications.Clear();

            _aircraft = new()
            {
                Address = Address,
                Altitude = Altitude,
                Latitude = Latitude,
                Longitude = Longitude,
                Distance = Distance,
                Behaviour = AircraftBehaviour.LevelFlight
            };

            _position = new()
            {
                Address = Address,
                Altitude = Altitude,
                Latitude = Latitude,
                Longitude = Longitude,
                Distance = Distance
            };
        }

        [TestMethod]
        public async Task SendNotificationWithPositionTestAsync()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftTrackerNotificationSender(_logger, behaviours, 50, 0, 45000);
            sender.SendAircraftNotification(_aircraft, _position, this, AircraftNotificationType.Updated, OnAircraftNotification);
            await Task.Delay(DelayMs);
            AssertCorrectNotificationSent(AircraftNotificationType.Updated, true);
        }

        [TestMethod]
        public async Task SendNotificationWithoutPositionTestAsync()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftTrackerNotificationSender(_logger, behaviours, 50, 0, 45000);
            sender.SendAircraftNotification(_aircraft, null, this, AircraftNotificationType.Updated, OnAircraftNotification);
            await Task.Delay(DelayMs);
            AssertCorrectNotificationSent(AircraftNotificationType.Updated, false);
        }

        [TestMethod]
        public async Task SendNotificationWithNoFilteringCriteriaTestAsync()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftTrackerNotificationSender(_logger, behaviours, null, null, null);
            sender.SendAircraftNotification(_aircraft, _position, this, AircraftNotificationType.Updated, OnAircraftNotification);
            await Task.Delay(DelayMs);
            AssertCorrectNotificationSent(AircraftNotificationType.Updated, true);
        }

        [TestMethod]
        public async Task SendNotificationWithAltitudeTooLowTestAsync()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftTrackerNotificationSender(_logger, behaviours, null, 40000, null);
            sender.SendAircraftNotification(_aircraft, null, this, AircraftNotificationType.Updated, OnAircraftNotification);
            await Task.Delay(DelayMs);
            Assert.IsEmpty(_notifications);
        }

        [TestMethod]
        public async Task SendNotificationWithAltitudeTooHighTestAsync()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftTrackerNotificationSender(_logger, behaviours, null, null, 25000);
            sender.SendAircraftNotification(_aircraft, null, this, AircraftNotificationType.Updated, OnAircraftNotification);
            await Task.Delay(DelayMs);
            Assert.IsEmpty(_notifications);
        }

        [TestMethod]
        public async Task SendNotificationWithDistanceTooHighTestAsync()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftTrackerNotificationSender(_logger, behaviours, 10, null, null);
            sender.SendAircraftNotification(_aircraft, null, this, AircraftNotificationType.Updated, OnAircraftNotification);
            await Task.Delay(DelayMs);
            Assert.IsEmpty(_notifications);
        }

        [TestMethod]
        public async Task SendNotificationWithMismatchingBehaviourTestAsync()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftTrackerNotificationSender(_logger, [AircraftBehaviour.Climbing, AircraftBehaviour.Descending], null, null, null);
            sender.SendAircraftNotification(_aircraft, null, this, AircraftNotificationType.Updated, OnAircraftNotification);
            await Task.Delay(DelayMs);
            Assert.IsEmpty(_notifications);
        }

        private void OnAircraftNotification(object sender, AircraftNotificationEventArgs e)
        {
            _notifications.Add(new AircraftNotificationData
            {
                Aircraft = e.Aircraft,
                Position = e.Position,
                NotificationType = e.NotificationType
            });
        }

        private void AssertCorrectNotificationSent(AircraftNotificationType type, bool expectPosition)
        {
            Assert.HasCount(1, _notifications);
            Assert.AreEqual(type, _notifications[0].NotificationType);
            Assert.AreEqual(_aircraft.Address, _notifications[0].Aircraft.Address);
            Assert.AreEqual(_aircraft.Altitude, _notifications[0].Aircraft.Altitude);
            Assert.AreEqual(_aircraft.Latitude, _notifications[0].Aircraft.Latitude);
            Assert.AreEqual(_aircraft.Longitude, _notifications[0].Aircraft.Longitude);
            Assert.AreEqual(_aircraft.Distance, _notifications[0].Aircraft.Distance);

            if (expectPosition)
            {
                Assert.AreEqual(_aircraft.Altitude, _notifications[0].Position.Altitude);
                Assert.AreEqual(_aircraft.Latitude, _notifications[0].Position.Latitude);
                Assert.AreEqual(_aircraft.Longitude, _notifications[0].Position.Longitude);
                Assert.AreEqual(_aircraft.Distance, _notifications[0].Position.Distance);
            }
            else
            {
                Assert.IsNull(_notifications[0].Position);
            }
        }
    }
}