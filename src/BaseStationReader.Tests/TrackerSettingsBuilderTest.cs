using BaseStationReader.Entities.Logging;
using BaseStationReader.Logic.Configuration;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class TrackerSettingsBuilderTest
    {
        [TestMethod]
        public void DefaultConfigTest()
        {
            var settings = new TrackerSettingsBuilder().BuildSettings(Array.Empty<string>(), "trackersettings.json");

            Assert.AreEqual("192.168.0.98", settings?.Host);
            Assert.AreEqual(30003, settings?.Port);
            Assert.AreEqual(60000, settings?.SocketReadTimeout);
            Assert.AreEqual(600000, settings?.ApplicationTimeout);
            Assert.AreEqual(60000, settings?.TimeToRecent);
            Assert.AreEqual(120000, settings?.TimeToStale);
            Assert.AreEqual(180000, settings?.TimeToRemoval);
            Assert.AreEqual(900000, settings?.TimeToLock);
            Assert.AreEqual("AircraftTracker.log", settings?.LogFile);
            Assert.AreEqual(Severity.Info, settings?.MinimumLogLevel);
            Assert.IsFalse(settings?.EnableSqlWriter);
            Assert.AreEqual(30000, settings?.WriterInterval);
            Assert.AreEqual(20000, settings?.WriterBatchSize);
            Assert.AreEqual(10000, settings?.RefreshInterval);
            Assert.AreEqual(20, settings?.MaximumRows);
            Assert.AreEqual("51.47", settings!.ReceiverLatitude.ToString("#.##"));
            Assert.AreEqual("-.45", settings!.ReceiverLongitude.ToString("#.##"));

            Assert.IsNotNull(settings?.Columns);
            Assert.AreEqual(1, settings?.Columns.Count);
            Assert.AreEqual("Latitude", settings?.Columns.First().Property);
            Assert.AreEqual("Lat", settings?.Columns.First().Label);
            Assert.AreEqual("N5", settings?.Columns.First().Format);
        }

        [TestMethod]
        public void OverrideHostTest()
        {
            var args = new string[] { "--host", "127.0.0.1" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual("127.0.0.1", settings?.Host);
        }

        [TestMethod]
        public void OverridePortTest()
        {
            var args = new string[] { "--port", "12345" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(12345, settings?.Port);
        }

        [TestMethod]
        public void OverrideSocketReadTimeoutTest()
        {
            var args = new string[] { "--read-timeout", "33456" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(33456, settings?.SocketReadTimeout);
        }

        [TestMethod]
        public void OverrideApplicationTimeoutTest()
        {
            var args = new string[] { "--app-timeout", "45198" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(45198, settings?.ApplicationTimeout);
        }

        [TestMethod]
        public void OverrideTimeToRecentTest()
        {
            var args = new string[] { "--recent", "25000" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(25000, settings?.TimeToRecent);
        }

        [TestMethod]
        public void OverrideTimeToStaleTest()
        {
            var args = new string[] { "--stale", "31000" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(31000, settings?.TimeToStale);
        }

        [TestMethod]
        public void OverrideTimeToRemovalTest()
        {
            var args = new string[] { "--remove", "39000" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(39000, settings?.TimeToRemoval);
        }

        [TestMethod]
        public void OverrideTimeToLockTest()
        {
            var args = new string[] { "--lock", "501896" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(501896, settings?.TimeToLock);
        }

        [TestMethod]
        public void OverrideLogFileTest()
        {
            var args = new string[] { "--log-file", "MyLog.log" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual("MyLog.log", settings?.LogFile);
        }

        [TestMethod]
        public void OverrideMinimumLogLevelTest()
        {
            var args = new string[] { "--log-level", "Debug" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(Severity.Debug, settings?.MinimumLogLevel);
        }

        [TestMethod]
        public void OverrideEnableSqlWriterTest()
        {
            var args = new string[] { "--enable-sql-writer", "true" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.IsTrue(settings?.EnableSqlWriter);
        }

        [TestMethod]
        public void OverrideWriterIntervalTest()
        {
            var args = new string[] { "--writer-interval", "15000" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(15000, settings?.WriterInterval);
        }

        [TestMethod]
        public void OverrideWriterBatchSizeTest()
        {
            var args = new string[] { "--writer-batch-size", "5000" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(5000, settings?.WriterBatchSize);
        }

        [TestMethod]
        public void OverrideWriterRefreshIntervalTest()
        {
            var args = new string[] { "--ui-interval", "45000" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(45000, settings?.RefreshInterval);
        }

        [TestMethod]
        public void OverrideMaximumRowsTest()
        {
            var args = new string[] { "--max-rows", "0" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(0, settings?.MaximumRows);
        }

        [TestMethod]
        public void OverrideReceiverLatitudeTest()
        {
            var args = new string[] { "--latitude", "58.93" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(58.93, Math.Round((double)settings!.ReceiverLatitude, 2, MidpointRounding.AwayFromZero));
        }

        [TestMethod]
        public void OverrideReceiverLongitueTest()
        {
            var args = new string[] { "--longitude", "120.56" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "trackersettings.json");
            Assert.AreEqual(120.56, Math.Round((double)settings!.ReceiverLongitude, 2, MidpointRounding.AwayFromZero));
        }
    }
}
