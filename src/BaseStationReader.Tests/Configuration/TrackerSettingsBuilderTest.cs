using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Config;

namespace BaseStationReader.Tests.Configuration
{
    [TestClass]
    public class TrackerSettingsBuilderTest : TrackerSettingsTestBase
    {
        private ITrackerSettingsBuilder _builder = new TrackerSettingsBuilder();
        private ICommandLineParser _parser = new TrackerCommandLineParser(null);
        private ITrackingProfileReaderWriter _reader = new TrackingProfileReaderWriter();

        [TestMethod]
        public void DefaultConfigTest()
        {
            _parser.Parse(Array.Empty<string>());
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            AssertCorrectSettings(settings);
        }

        [TestMethod]
        public void OverrideDefaultConfigFileTest()
        {
            var args = new string[] { "--settings", "alternatesettings.json" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "");
            AssertCorrectSettings(settings);
        }

        [TestMethod]
        public void OverrideHostTest()
        {
            var args = new string[] { "--host", "127.0.0.1" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual("127.0.0.1", settings.Host);
        }

        [TestMethod]
        public void OverridePortTest()
        {
            var args = new string[] { "--port", "12345" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(12345, settings.Port);
        }

        [TestMethod]
        public void OverrideSocketReadTimeoutTest()
        {
            var args = new string[] { "--read-timeout", "33456" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(33456, settings.SocketReadTimeout);
        }

        [TestMethod]
        public void OverrideApplicationTimeoutTest()
        {
            var args = new string[] { "--app-timeout", "45198" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(45198, settings.ApplicationTimeout);
        }

        [TestMethod]
        public void OverrideRestartOnTimeoutTest()
        {
            var args = new string[] { "--auto-restart", "false" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.IsFalse(settings.RestartOnTimeout);
        }

        [TestMethod]
        public void OverrideTimeToRecentTest()
        {
            var args = new string[] { "--recent", "25000" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(25000, settings.TimeToRecent);
        }

        [TestMethod]
        public void OverrideTimeToStaleTest()
        {
            var args = new string[] { "--stale", "31000" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(31000, settings.TimeToStale);
        }

        [TestMethod]
        public void OverrideTimeToRemovalTest()
        {
            var args = new string[] { "--remove", "39000" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(39000, settings.TimeToRemoval);
        }

        [TestMethod]
        public void OverrideTimeToLockTest()
        {
            var args = new string[] { "--lock", "501896" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(501896, settings.TimeToLock);
        }

        [TestMethod]
        public void OverrideLogFileTest()
        {
            var args = new string[] { "--log-file", "MyLog.log" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual("MyLog.log", settings.LogFile);
        }

        [TestMethod]
        public void OverrideMinimumLogLevelTest()
        {
            var args = new string[] { "--log-level", "Debug" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(Severity.Debug, settings.MinimumLogLevel);
        }

        [TestMethod]
        public void OverrideEnableSqlWriterTest()
        {
            var args = new string[] { "--enable-sql-writer", "true" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.IsTrue(settings.EnableSqlWriter);
        }

        [TestMethod]
        public void OverrideWriterIntervalTest()
        {
            var args = new string[] { "--writer-interval", "15000" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(15000, settings.WriterInterval);
        }

        [TestMethod]
        public void OverrideWriterBatchSizeTest()
        {
            var args = new string[] { "--writer-batch-size", "5000" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(5000, settings.WriterBatchSize);
        }

        [TestMethod]
        public void OverrideMaximumRowsTest()
        {
            var args = new string[] { "--max-rows", "0" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(0, settings.MaximumRows);
        }

        [TestMethod]
        public void OverrideReceiverLatitudeTest()
        {
            var args = new string[] { "--latitude", "58.93" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(58.93, Math.Round((double)settings.ReceiverLatitude, 2, MidpointRounding.AwayFromZero));
        }

        [TestMethod]
        public void OverrideReceiverLongitueTest()
        {
            var args = new string[] { "--longitude", "120.56" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(120.56, Math.Round((double)settings.ReceiverLongitude, 2, MidpointRounding.AwayFromZero));
        }

        [TestMethod]
        public void OverrideMaximumTrackedDistanceTest()
        {
            var args = new string[] { "--max-distance", "23" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(23, settings.MaximumTrackedDistance);
        }

        [TestMethod]
        public void OverrideMinimumTrackedAltitudeTest()
        {
            var args = new string[] { "--min-altitude", "4000" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(4000, settings.MinimumTrackedAltitude);
        }

        [TestMethod]
        public void OverrideMaximumTrackedAltitudeTest()
        {
            var args = new string[] { "--max-altitude", "12000" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual(12000, settings.MaximumTrackedAltitude);
        }

        [TestMethod]
        public void OverrideTrackedBehavioursTest()
        {
            var args = new string[] { "--behaviours", "Descending" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");

            Assert.HasCount(1, settings.TrackedBehaviours);
            Assert.AreEqual(AircraftBehaviour.Descending, settings.TrackedBehaviours[0]);
        }

        [TestMethod]
        public void OverrideClearDownTest()
        {
            var args = new string[] { "--cleardown", "true" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.IsTrue(settings.ClearDown);
        }

        [TestMethod]
        public void OverrideAutoLookupTest()
        {
            var args = new string[] { "--auto-lookup", "true" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.IsTrue(settings.AutoLookup);
        }

        [TestMethod]
        public void OverrideTrackPositionTest()
        {
            var args = new string[] { "--track-position", "false" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.IsFalse(settings.TrackPosition);
        }

        [TestMethod]
        public void SpecifyTrackingProfileTest()
        {
            var args = new string[] { "--tracking-profile", "LHR-Landing.json" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");

            Assert.AreEqual("51.471", settings.ReceiverLatitude?.ToString("#.###"));
            Assert.AreEqual("-.462", settings.ReceiverLongitude?.ToString("#.###"));
            Assert.AreEqual(15, settings.MaximumTrackedDistance);
            Assert.AreEqual(200, settings.MinimumTrackedAltitude);
            Assert.AreEqual(5000, settings.MaximumTrackedAltitude);
            Assert.HasCount(1, settings.TrackedBehaviours);
            Assert.AreEqual(AircraftBehaviour.Descending, settings.TrackedBehaviours[0]);
        }

        [TestMethod]
        public void OverrideFlightApiTest()
        {
            var args = new string[] { "--flight-api", "Missing" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.AreEqual("Missing", settings.FlightApi);
        }

        [TestMethod]
        public void OverrideVerboseLoggingTest()
        {
            var args = new string[] { "--verbose", "true" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, _reader, "trackersettings.json");
            Assert.IsTrue(settings.VerboseLogging);
        }
    }
}
