using BaseStationReader.Data;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Tests.Mocks;
using System.Diagnostics;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.BusinessLogic.Events;

namespace BaseStationReader.Tests.Database
{
    [TestClass]
    public class QueuedWriterTest
    {
        private const int WriterInterval = 10;
        private const int WriterBatchSize = 100;
        private const int TimeToLockMs = 500;
        private const int MaximumWriterWaitTimeMs = 1200;

        private const string Address = "406A3D";
        private const string Callsign = "BAW486";
        private const decimal Altitude = 14325.0M;
        private const decimal GroundSpeed = 362.0M;
        private const decimal Track = 168.0M;
        private const decimal Latitude = 51.15067M;
        private const decimal Longitude = -0.52048M;
        private const decimal VerticalRate = 2624.0M;
        private const string Squawk = "7710";

        private IDatabaseManagementFactory _factory = null;
        private QueuedWriter _writer = null;
        private bool _queueProcessed = false;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            // Create a database management factory to supply entity management classes
            var logger = new MockFileLogger();
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, context, TimeToLockMs, 0);

            // Create a queued writer, wire up the event handlers and start it
            var writerTimer = new MockTrackerTimer(WriterInterval);
            var sender = new QueuedWriterNotificationSender(logger);
            _writer = new QueuedWriter(_factory, null, writerTimer, sender, [], [], WriterBatchSize, true);
            _writer.BatchCompleted += OnBatchWritten;

            // Start the writer
            await _writer.StartAsync();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _writer.Stop();
        }

        [TestMethod]
        public async Task AddNewAircraftTestAsync()
        {
            Push(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            });

            WaitForQueueToEmpty();

            var aircraft = await _factory.TrackedAircraftWriter.GetAsync(x => x.Address == Address);
            Assert.IsNotNull(aircraft);
            Assert.AreEqual(Address, aircraft.Address);
        }

        [TestMethod]
        public async Task UpdateExistingAircraftTestAsync()
        {
            var added =  await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            });

            Push(new TrackedAircraft
            {
                Address = Address,
                Callsign = Callsign,
                Altitude = Altitude,
                GroundSpeed = GroundSpeed,
                Track = Track,
                Latitude = Latitude,
                Longitude = Longitude,
                VerticalRate = VerticalRate,
                Squawk = Squawk,
                FirstSeen = added.FirstSeen,
                LastSeen = added.LastSeen
            });

            WaitForQueueToEmpty();

            var aircraft = Task.Run(() => _factory.TrackedAircraftWriter.ListAsync(x => x.Address == Address)).Result;
            Assert.IsNotNull(aircraft);
            Assert.HasCount(1, aircraft);
            Assert.AreEqual(added.Id, aircraft[0].Id);
            Assert.AreEqual(Address, aircraft[0].Address);
            Assert.AreEqual(Altitude, aircraft[0].Altitude);
            Assert.AreEqual(GroundSpeed, aircraft[0].GroundSpeed);
            Assert.AreEqual(Track, aircraft[0].Track);
            Assert.AreEqual(Latitude, aircraft[0].Latitude);
            Assert.AreEqual(Longitude, aircraft[0].Longitude);
            Assert.AreEqual(VerticalRate, aircraft[0].VerticalRate);
            Assert.AreEqual(Squawk, aircraft[0].Squawk);
            Assert.AreEqual(added.FirstSeen, aircraft[0].FirstSeen);
            Assert.AreEqual(added.LastSeen, aircraft[0].LastSeen);
        }

        [TestMethod]
        public async Task AddActivePositionTestAsync()
        {
            var aircraft = await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            });

            Push(new AircraftPosition
            {
                Address = Address,
                Altitude = Altitude,
                Latitude = Latitude,
                Longitude = Longitude,
                Timestamp = DateTime.Now
            });

            WaitForQueueToEmpty();

            var position = Task.Run(() => _factory.PositionWriter.GetAsync(x => x.AircraftId == aircraft.Id)).Result;
            Assert.IsNotNull(position);
            Assert.IsGreaterThan(0, position.Id);
            Assert.AreEqual(aircraft.Id, position.AircraftId);
            Assert.AreEqual(Altitude, position.Altitude);
            Assert.AreEqual(Latitude, position.Latitude);
            Assert.AreEqual(Longitude, position.Longitude);
        }

        [TestMethod]
        public async Task StaleRecordIsLockedOnUpdateTestAsync()
        {
            await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-20),
                LastSeen = DateTime.Now.AddMinutes(-15)
            });

            var added = Task.Run(() => _factory.TrackedAircraftWriter.GetAsync(x => x.Address == Address)).Result;
            Assert.IsNotNull(added);
            Assert.IsGreaterThan(0, added.Id);
            Assert.AreNotEqual(TrackingStatus.Locked, added.Status);

            Push(new TrackedAircraft
            {
                Address = Address,
                Callsign = Callsign,
                Altitude = Altitude,
                GroundSpeed = GroundSpeed,
                Track = Track,
                Latitude = Latitude,
                Longitude = Longitude,
                VerticalRate = VerticalRate,
                Squawk = Squawk,
                FirstSeen = DateTime.Now.AddMinutes(-20),
                LastSeen = DateTime.Now
            });

            WaitForQueueToEmpty();

            var aircraft = Task.Run(() => _factory.TrackedAircraftWriter.ListAsync(x => true)).Result;
            Assert.IsNotNull(aircraft);
            Assert.HasCount(2, aircraft);
            Assert.IsGreaterThan(0, aircraft[0].Id);
            Assert.AreNotEqual(added.Id, aircraft[0].Id);
            Assert.AreEqual(added.Id, aircraft[1].Id);
        }

        [TestMethod]
        public async Task NewSessionLocksAllTestAsync()
        {
            await _factory.TrackedAircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            });

            var aircraft = await _factory.TrackedAircraftWriter.GetAsync(x => x.Address == Address);
            Assert.IsNotNull(aircraft);
            Assert.IsGreaterThan(0, aircraft.Id);
            Assert.AreNotEqual(TrackingStatus.Locked, aircraft.Status);

            _writer.Stop();
            await _writer.StartAsync();
            WaitForQueueToEmpty();

            var locked = await _factory.TrackedAircraftWriter.GetAsync(x => x.Address == Address);
            Assert.IsNotNull(locked);
            Assert.AreEqual(aircraft.Id, locked.Id);
            Assert.AreEqual(TrackingStatus.Locked, aircraft.Status);
        }

        [TestMethod]
        public async Task FlushQueueTestAsync()
        {
            Push(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            });

            Assert.AreEqual(1, _writer.QueueSize);
            await _writer.FlushQueueAsync();
            Assert.AreEqual(0, _writer.QueueSize);
        }

        [TestMethod]
        public void ClearQueueTest()
        {
            Push(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            });

            Assert.AreEqual(1, _writer.QueueSize);
            _writer.ClearQueue();
            Assert.AreEqual(0, _writer.QueueSize);
        }

        /// <summary>
        /// Push an entity into the writer's queue
        /// </summary>
        /// <param name="entity"></param>
        private void Push(object entity)
        {
            // Reset the processing flag
            _queueProcessed = false;

            // If the supplied object isn't null, push it into the queue
            if (entity != null)
            {
                _writer.Push(entity);
            }
        }

        /// <summary>
        /// Wait for the queued writer to process the pending writes
        /// </summary>
        private void WaitForQueueToEmpty()
        {
            // Start a stopwatch to end the test in case something goes awry
            var stopwatch = new Stopwatch();
            stopwatch.Start();
            while (!_queueProcessed && (stopwatch.ElapsedMilliseconds <= MaximumWriterWaitTimeMs))
            {
            }
            stopwatch.Stop();

            Assert.IsLessThanOrEqualTo(MaximumWriterWaitTimeMs, stopwatch.ElapsedMilliseconds);
        }

        /// <summary>
        /// Handle the event sent when a queued batch is processed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBatchWritten(object sender, BatchCompletedEventArgs e)
        {
            if ((e.InitialQueueSize > 0) && (e.FinalQueueSize == 0))
            {
                _queueProcessed = true;
            }
        }
    }
}
