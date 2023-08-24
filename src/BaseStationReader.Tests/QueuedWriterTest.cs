using BaseStationReader.Data;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace BaseStationReader.Tests
{
    [ExcludeFromCodeCoverage]
    [TestClass]
    public class QueuedWriterTest
    {
        private const int WriterInterval = 500;
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

        private QueuedWriter? _writer = null;
        private AircraftManager? _manager = null;
        private bool _queueProcessed = false;

        [TestMethod]
        public async Task TestQueuedWriter()
        {
            // Create an in-memory database context and an aircraft manager
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _manager = new AircraftManager(context);

            // Create a queued writer, wire up the event handlers and start it
            _writer = new QueuedWriter(_manager, WriterInterval, WriterBatchSize);
            _writer.BatchWritten += OnBatchWritten;
            _writer.Start();

            // Push an update into the queue and wait until it's been processed
            PushAndWait(new Aircraft
            {
                Address = Address,
                FirstSeen = FirstSeen,
                LastSeen = FirstSeen
            });

            // Check the state of the database
            await ConfirmAircraftProperties(FirstSeen, false, false);

            // Push a second update into the queue and wait until that's been processed, too
            PushAndWait(new Aircraft
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

            // Stop the writer
            _writer.Stop();

            // Check the state of the database
            await ConfirmAircraftProperties(LastSeen, true, false);

            // Restart the writer and wait for the queue (that it creates on start) to be processed
            _writer.Start();
            PushAndWait(null);

            // Check the state of the database
            await ConfirmAircraftProperties(LastSeen, true, true);

            // Stop the writer
            _writer.Stop();
        }

        private async Task ConfirmAircraftProperties(DateTime expectedLastSeen, bool checkUpdatedProperties, bool expectedLocked)
        {
            var aircraft = await _manager.ListAsync(x => x.Address == x.Address);
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
        /// Push an aircraft update to the writer queue and wait for it to be written
        /// </summary>
        /// <param name="aircraft"></param>
        private void PushAndWait(Aircraft? aircraft)
        {
            // Reset the processing flag
            _queueProcessed = false;

            // If the supplied aircraft isn't null, push it into the queu
            if (aircraft != null)
            {
                _writer?.Push(aircraft);
            }

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
