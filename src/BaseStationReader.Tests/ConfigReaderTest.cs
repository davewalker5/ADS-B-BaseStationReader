using BaseStationReader.Logic;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class ConfigReaderTest
    {
        [TestMethod]
        public void ReadAppSettingsTest()
        {
            var settings = ConfigReader.Read("appsettings.json");

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
    }
}
