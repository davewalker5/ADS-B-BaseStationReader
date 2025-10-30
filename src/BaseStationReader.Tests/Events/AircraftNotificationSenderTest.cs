using BaseStationReader.BusinessLogic.Events;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Tests.Entities;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.Tracking
{
    [TestClass]
    public class AircraftNotificationSenderTest
    {
        private const string Address = "485876";
        private const decimal Altitude = 29000M;
        private const decimal Latitude = 51.68825M;
        private const decimal Longitude = 51.68825M;
        private const double Distance = 24;

        private readonly ITrackerLogger _logger = new MockFileLogger();
        private List<AircraftNotificationData> _notifications = [];
        private TrackedAircraft _aircraft;

        [TestInitialize]
        public void Initialise()
        {
            _notifications.Clear();
            _aircraft = new TrackedAircraft()
            {
                Address = Address,
                Altitude = Altitude,
                Latitude = Latitude,
                Longitude = Longitude,
                Distance = Distance,
                Behaviour = AircraftBehaviour.LevelFlight
            };

        }

        [TestMethod]
        public void SendAddNotificationWithNoCriteriaTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, null, null, null, true);
            sender.SendAddedNotification(_aircraft, this, OnAircraftNotification);
            AssertCorrectNotificationSent(AircraftNotificationType.Added, false);
        }

        [TestMethod]
        public void SendAddNotificationWithNoDistanceCriteriaTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, null, 0, 40000, true);
            sender.SendAddedNotification(_aircraft, this, OnAircraftNotification);
            AssertCorrectNotificationSent(AircraftNotificationType.Added, false);
        }

        [TestMethod]
        public void SendAddNotificationWithNoMinimumAltitudeCriteriaTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, null, 40000, true);
            sender.SendAddedNotification(_aircraft, this, OnAircraftNotification);
            AssertCorrectNotificationSent(AircraftNotificationType.Added, false);
        }

        [TestMethod]
        public void SendAddNotificationWithNoMaximumAltitudeCriteriaTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, 0, null, true);
            sender.SendAddedNotification(_aircraft, this, OnAircraftNotification);
            AssertCorrectNotificationSent(AircraftNotificationType.Added, false);
        }

        [TestMethod]
        public void SendAddNotificationWithMatchingCriteriaTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, 0, 40000, true);
            sender.SendAddedNotification(_aircraft, this, OnAircraftNotification);
            AssertCorrectNotificationSent(AircraftNotificationType.Added, false);
        }

        [TestMethod]
        public void SendAddNotificationWithMisMatchingBehaviourTest()
        {
            List<AircraftBehaviour> behaviours = [AircraftBehaviour.Descending];
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, 0, 40000, true);
            sender.SendAddedNotification(_aircraft, this, OnAircraftNotification);
            Assert.HasCount(0, _notifications);
        }

        [TestMethod]
        public void SendAddNotificationWithDistanceTooFarTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 10, 0, 40000, true);
            sender.SendAddedNotification(_aircraft, this, OnAircraftNotification);
            Assert.HasCount(0, _notifications);
        }

        [TestMethod]
        public void SendAddNotificationWithAltitudeTooLowTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, 30000, 40000, true);
            sender.SendAddedNotification(_aircraft, this, OnAircraftNotification);
            Assert.HasCount(0, _notifications);
        }

        [TestMethod]
        public void SendAddNotificationWithAltitudeTooHighTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, 0, 20000, true);
            sender.SendAddedNotification(_aircraft, this, OnAircraftNotification);
            Assert.HasCount(0, _notifications);
        }

        [TestMethod]
        public void SendUpdatedNotificationWithMatchingCriteriaWithPositionTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, 0, 40000, true);
            sender.SendUpdatedNotification(_aircraft, this, OnAircraftNotification, Latitude - 0.5M, Longitude - 0.5M, Altitude - 1000, Distance - 10);
            AssertCorrectNotificationSent(AircraftNotificationType.Updated, true);
        }

        [TestMethod]
        public void SendUpdatedNotificationWithMatchingCriteriaWithoutPositionTrackingTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, 0, 40000, false);
            sender.SendUpdatedNotification(_aircraft, this, OnAircraftNotification, Latitude - 0.5M, Longitude - 0.5M, Altitude - 1000, Distance - 10);
            AssertCorrectNotificationSent(AircraftNotificationType.Updated, false);
        }

        [TestMethod]
        public void SendUpdatedNotificationWthMatchingCriteriaWithNoChangeInPositionTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, 0, 40000, true);
            sender.SendUpdatedNotification(_aircraft, this, OnAircraftNotification, Latitude, Longitude, Altitude, Distance);
            AssertCorrectNotificationSent(AircraftNotificationType.Updated, false);
        }

        [TestMethod]
        public void SendUpdatedNotificationWthMatchingCriteriaWithInvalidPositionTest()
        {
            _aircraft.Latitude = _aircraft.Longitude = _aircraft.Altitude = null;
            _aircraft.Distance = null;
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, null, null, null, true);
            sender.SendUpdatedNotification(_aircraft, this, OnAircraftNotification, Latitude, Longitude, Altitude, Distance);
            AssertCorrectNotificationSent(AircraftNotificationType.Updated, false);
        }

        [TestMethod]
        public void SendStaleNotificationWithMatchingCriteriaTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, 0, 40000, true);
            sender.SendStaleNotification(_aircraft, this, OnAircraftNotification);
            AssertCorrectNotificationSent(AircraftNotificationType.Stale, false);
        }

        [TestMethod]
        public void SendInactiveNotificationWithMatchingCriteriaTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, 0, 40000, true);
            sender.SendInactiveNotification(_aircraft, this, OnAircraftNotification);
            AssertCorrectNotificationSent(AircraftNotificationType.Recent, false);
        }

        [TestMethod]
        public void SendRemovedNotificationWithMatchingCriteriaTest()
        {
            var behaviours = Enum.GetValues<AircraftBehaviour>();
            var sender = new AircraftNotificationSender(_logger, behaviours, 100, 0, 40000, true);
            sender.SendRemovedNotification(_aircraft, this, OnAircraftNotification);
            AssertCorrectNotificationSent(AircraftNotificationType.Removed, false);
        }

        private void OnAircraftNotification(object sender, AircraftNotificationEventArgs e)
        {
            lock (_notifications)
            {
                _notifications.Add(new AircraftNotificationData
                {
                    Aircraft = e.Aircraft,
                    Position = e.Position,
                    NotificationType = e.NotificationType
                });
            }
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