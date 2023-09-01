using BaseStationReader.Data;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic;
using BaseStationReader.Tests.Mocks;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BaseStationReader.Tests
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class QueuedWriterTest
    {
        private const int WriterInterval = 10;
        private const int WriterBatchSize = 100;
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
        private readonly DateTime FirstSeen = DateTime.ParseExact("2023-08-22 17:51:59.551", "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);
        private readonly DateTime LastSeen = DateTime.ParseExact("2023-08-22 17:56:24.909", "yyyy-MM-dd HH:mm:ss.fff", CultureInfo.InvariantCulture);

        private BaseStationReaderDbContext? _context = null;
        private AircraftWriter? _aircraftWriter = null;
        private PositionWriter? _positionWriter = null;
        private QueuedWriter? _writer = null;
        private bool _queueProcessed = false;

        [TestInitialize]
        public void TestInitialise()
        {
            // Create an in-memory database context and the two writers
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _aircraftWriter = new AircraftWriter(_context);
            _positionWriter = new PositionWriter(_context);

            // Create a queued writer, wire up the event handlers and start it
            var logger = new MockFileLogger();
            var writerTimer = new MockTrackerTimer(WriterInterval);
            _writer = new QueuedWriter(_aircraftWriter, _positionWriter, logger, writerTimer, WriterBatchSize);
            _writer.BatchWritten += OnBatchWritten;
        }

        [TestMethod]
        public async Task TestAircraftWriter()
        {
            // Start the writer
            _writer!.Start();

            // Push an aircraft update into the queue
            Push(new Aircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = FirstSeen
            });

            // Wait for the queue to be processed then check the state of the database
            WaitForQueueToEmpty();
            await ConfirmAircraftProperties(FirstSeen, false, false);

            // Push a second update into the queue
            Push(new Aircraft
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
                FirstSeen = FirstSeen,
                LastSeen = LastSeen
            });

            // Wait for the queue to be processed then check the state of the database
            WaitForQueueToEmpty();
            await ConfirmAircraftProperties(LastSeen, true, false);

            // Stop the writer
            _writer.Stop();

            // Restart the writer and wait for the queue (that it creates on start) to be processed
            _writer.Start();
            WaitForQueueToEmpty();

            // Check the state of the database
            await ConfirmAircraftProperties(LastSeen, true, true);

            // Stop the writer
            _writer.Stop();
        }

        [TestMethod]
        public async Task TestPositionWriter()
        {
            // Start the writer
            _writer!.Start();

            // Push an aircraft update into the queue
            Push(new Aircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = FirstSeen
            });

            // Push a position update into the queue
            Push(new AircraftPosition
            {
                Address = Address,
                Altitude = Altitude,
                Latitude = Latitude,
                Longitude = Longitude,
                Timestamp = DateTime.Now
            });

            // Wait for the queue to empty
            WaitForQueueToEmpty();

            // Check the state of the database
            var positions = await _positionWriter.ListAsync(x => true);
            Assert.IsNotNull(positions);
            Assert.AreEqual(1, positions.Count);
            Assert.AreEqual(Altitude, positions.First().Altitude);
            Assert.AreEqual(Latitude, positions.First().Latitude);
            Assert.AreEqual(Longitude, positions.First().Longitude);

            // Stop the writer
            _writer.Stop();
        }


        /// <summary>
        /// Confirm the properties of the aircraft are as expected
        /// </summary>
        /// <param name="expectedLastSeen"></param>
        /// <param name="checkUpdatedProperties"></param>
        /// <param name="expectedLocked"></param>
        /// <returns></returns>
        private async Task ConfirmAircraftProperties(DateTime expectedLastSeen, bool checkUpdatedProperties, bool expectedLocked)
        {
#pragma warning disable CS8602
            var aircraft = await _aircraftWriter.ListAsync(x => true);
#pragma warning restore CS8602
            Assert.IsNotNull(aircraft);
            Assert.AreEqual(1, aircraft.Count);
            Assert.AreEqual(Address, aircraft.First().Address);
            Assert.AreEqual(FirstSeen, aircraft.First().FirstSeen);
            Assert.AreEqual(expectedLastSeen, aircraft.First().LastSeen);

            if (expectedLocked)
            {
                Assert.IsTrue(aircraft.First().Locked);
            }
            else
            {
                Assert.IsFalse(aircraft.First().Locked);
            }

            if (checkUpdatedProperties)
            {
                Assert.AreEqual(Altitude, aircraft.First().Altitude);
                Assert.AreEqual(GroundSpeed, aircraft.First().GroundSpeed);
                Assert.AreEqual(Track, aircraft.First().Track);
                Assert.AreEqual(Latitude, aircraft.First().Latitude);
                Assert.AreEqual(Longitude, aircraft.First().Longitude);
                Assert.AreEqual(VerticalRate, aircraft.First().VerticalRate);
                Assert.AreEqual(Squawk, aircraft.First().Squawk);
            }
        }

        /// <summary>
        /// Push an arcraft update or position to the writer queue
        /// </summary>
        /// <param name="entity"></param>
        private void Push(object entity)
        {
            // Reset the processing flag
            _queueProcessed = false;

            // If the supplied aircraft isn't null, push it into the queu
            if (entity != null)
            {
                _writer!.Push(entity);
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
        }

        /// <summary>
        /// Handle the event sent when a queued batch is processed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBatchWritten(object? sender, BatchWrittenEventArgs e)
        {
            if ((e.InitialQueueSize > 0) && (e.FinalQueueSize == 0))
            {
                _queueProcessed = true;
            }
        }
    }
}
