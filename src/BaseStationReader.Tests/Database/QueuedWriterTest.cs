using BaseStationReader.Data;
using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Tests.Mocks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Interfaces.Database;

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

        private BaseStationReaderDbContext _context = null;
        private ITrackedAircraftWriter _aircraftWriter = null;
        private IPositionWriter _positionWriter = null;
        private IAircraftLockManager _aircraftLocker = null;
        private QueuedWriter _writer = null;
        private bool _queueProcessed = false;

        [TestInitialize]
        public async Task TestInitialise()
        {
            // Create an in-memory database context, the two writers and a lock manager
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _aircraftWriter = new TrackedAircraftWriter(_context);
            _positionWriter = new PositionWriter(_context);
            _aircraftLocker = new AircraftLockManager(_aircraftWriter, TimeToLockMs);

            // Create a queued writer, wire up the event handlers and start it
            var logger = new MockFileLogger();
            var writerTimer = new MockTrackerTimer(WriterInterval);
            _writer = new QueuedWriter(
                _aircraftWriter, _positionWriter, _aircraftLocker, null, logger, writerTimer, [], [], WriterBatchSize, true);
            _writer.BatchWritten += OnBatchWritten;

            // Start the writer
            await _writer.StartAsync();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            _writer.Stop();
        }

        [TestMethod]
        public void AddNewAircraftTest()
        {
            Push(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            });

            WaitForQueueToEmpty();

            var aircraft = Task.Run(() => _aircraftWriter.GetAsync(x => x.Address == Address)).Result;
            Assert.IsNotNull(aircraft);
            Assert.AreEqual(Address, aircraft.Address);
        }

        [TestMethod]
        public void UpdateExistingAircraftTest()
        {
            var added =  Task.Run(() => _aircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            })).Result;

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

            var aircraft = Task.Run(() => _aircraftWriter.ListAsync(x => x.Address == Address)).Result;
            Assert.IsNotNull(aircraft);
            Assert.AreEqual(1, aircraft.Count);
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
        public void AddActivePositionTest()
        {
            var aircraft = Task.Run(() => _aircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            })).Result;

            Push(new AircraftPosition
            {
                Address = Address,
                Altitude = Altitude,
                Latitude = Latitude,
                Longitude = Longitude,
                Timestamp = DateTime.Now
            });

            WaitForQueueToEmpty();

            var position = Task.Run(() => _positionWriter.GetAsync(x => x.AircraftId == aircraft.Id)).Result;
            Assert.IsNotNull(position);
            Assert.IsTrue(position.Id > 0);
            Assert.AreEqual(aircraft.Id, position.AircraftId);
            Assert.AreEqual(Altitude, position.Altitude);
            Assert.AreEqual(Latitude, position.Latitude);
            Assert.AreEqual(Longitude, position.Longitude);
        }

        [TestMethod]
        public void StaleRecordIsLockedOnUpdateTest()
        {
            Task.Run(() => _aircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-20),
                LastSeen = DateTime.Now.AddMinutes(-15)
            })).Wait();

            var added = Task.Run(() => _aircraftWriter.GetAsync(x => x.Address == Address)).Result;
            Assert.IsNotNull(added);
            Assert.IsTrue(added.Id > 0);
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

            var aircraft = Task.Run(() => _aircraftWriter.ListAsync(x => true)).Result;
            Assert.IsNotNull(aircraft);
            Assert.AreEqual(2, aircraft.Count);
            Assert.IsTrue(aircraft[0].Id > 0);
            Assert.AreNotEqual(added.Id, aircraft[0].Id);
            Assert.AreEqual(added.Id, aircraft[1].Id);
        }

        [TestMethod]
        public async Task NewSessionLocksAllTest()
        {
            Task.Run(() => _aircraftWriter.WriteAsync(new TrackedAircraft
            {
                Address = Address,
                FirstSeen = DateTime.Now.AddMinutes(-10),
                LastSeen = DateTime.Now
            })).Wait();

            var aircraft = Task.Run(() => _aircraftWriter.GetAsync(x => x.Address == Address)).Result;
            Assert.IsNotNull(aircraft);
            Assert.IsTrue(aircraft.Id > 0);
            Assert.AreNotEqual(TrackingStatus.Locked, aircraft.Status);

            _writer.Stop();
            await _writer.StartAsync();
            WaitForQueueToEmpty();

            var locked = Task.Run(() => _aircraftWriter.GetAsync(x => x.Address == Address)).Result;
            Assert.IsNotNull(locked);
            Assert.AreEqual(aircraft.Id, locked.Id);
            Assert.AreEqual(TrackingStatus.Locked, aircraft.Status);
        }

        /// <summary>
        /// Push an entity into the writer's queue
        /// </summary>
        /// <param name="entity"></param>
        private void Push(object entity)
        {
            // Reset the processing flag
            _queueProcessed = false;

            // If the supplied aircraft isn't null, push it into the queu
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

            Assert.IsTrue(stopwatch.ElapsedMilliseconds <= MaximumWriterWaitTimeMs);
        }

        /// <summary>
        /// Handle the event sent when a queued batch is processed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBatchWritten(object sender, BatchWrittenEventArgs e)
        {
            if ((e.InitialQueueSize > 0) && (e.FinalQueueSize == 0))
            {
                _queueProcessed = true;
            }
        }
    }
}
