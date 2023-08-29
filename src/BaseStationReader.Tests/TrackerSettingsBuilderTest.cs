using BaseStationReader.Terminal.Logic;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class TrackerSettingsBuilderTest
    {
        [TestMethod]
        public void DefaultConfigTest()
        {
            var settings = new TrackerSettingsBuilder().BuildSettings(Array.Empty<string>(), "appsettings.json");

            Assert.AreEqual("192.168.0.98", settings?.Host);
            Assert.AreEqual(30003, settings?.Port);
            Assert.AreEqual(60000, settings?.SocketReadTimeout);
            Assert.AreEqual(600000, settings?.ApplicationTimeout);
            Assert.AreEqual(60000, settings?.TimeToRecent);
            Assert.AreEqual(120000, settings?.TimeToStale);
            Assert.AreEqual(180000, settings?.TimeToRemoval);
            Assert.AreEqual("AircraftTracker.log", settings?.LogFile);
            Assert.IsFalse(settings?.EnableSqlWriter);
            Assert.AreEqual(30000, settings?.WriterInterval);
            Assert.AreEqual(20000, settings?.WriterBatchSize);
            Assert.AreEqual(20, settings?.MaximumRows);

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
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.AreEqual("127.0.0.1", settings?.Host);
        }

        [TestMethod]
        public void OverridePortTest()
        {
            var args = new string[] { "--port", "12345" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.AreEqual(12345, settings?.Port);
        }

        [TestMethod]
        public void OverrideSocketReadTimeoutTest()
        {
            var args = new string[] { "--read-timeout", "33456" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.AreEqual(33456, settings?.SocketReadTimeout);
        }

        [TestMethod]
        public void OverrideApplicationTimeoutTest()
        {
            var args = new string[] { "--app-timeout", "45198" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.AreEqual(45198, settings?.ApplicationTimeout);
        }

        [TestMethod]
        public void OverrideTimeToRecentTest()
        {
            var args = new string[] { "--recent", "25000" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.AreEqual(25000, settings?.TimeToRecent);
        }

        [TestMethod]
        public void OverrideTimeToStaleTest()
        {
            var args = new string[] { "--stale", "31000" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.AreEqual(31000, settings?.TimeToStale);
        }

        [TestMethod]
        public void OverrideTimeToRemovalTest()
        {
            var args = new string[] { "--remove", "39000" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.AreEqual(39000, settings?.TimeToRemoval);
        }

        [TestMethod]
        public void OverrideLogFileTest()
        {
            var args = new string[] { "--log-file", "MyLog.log" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.AreEqual("MyLog.log", settings?.LogFile);
        }

        [TestMethod]
        public void OverrideEnableSqlWriterTest()
        {
            var args = new string[] { "--enable-sql-writer", "true" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.IsTrue(settings?.EnableSqlWriter);
        }

        [TestMethod]
        public void OverrideWriterIntervalTest()
        {
            var args = new string[] { "--writer-interval", "15000" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.AreEqual(15000, settings?.WriterInterval);
        }

        [TestMethod]
        public void OverrideWriterBatchSizeTest()
        {
            var args = new string[] { "--writer-batch-size", "5000" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.AreEqual(5000, settings?.WriterBatchSize);
        }

        [TestMethod]
        public void OverrideMaximumRowsTest()
        {
            var args = new string[] { "--max-rows", "0" };
            var settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");
            Assert.AreEqual(0, settings?.MaximumRows);
        }
    }
}
