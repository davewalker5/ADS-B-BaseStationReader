using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Logic.Configuration;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class ConfigReaderTest
    {
        [TestMethod]
        public void ReadAppSettingsTest()
        {
            var settings = new TrackerConfigReader().Read("trackersettings.json");

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
            Assert.AreEqual("51.47",settings!.ReceiverLatitude?.ToString("#.##"));
            Assert.AreEqual("-.45", settings!.ReceiverLongitude?.ToString("#.##"));

            Assert.IsNotNull(settings?.Columns);
            Assert.AreEqual(1, settings?.Columns.Count);
            Assert.AreEqual("Latitude", settings?.Columns.First().Property);
            Assert.AreEqual("Lat", settings?.Columns.First().Label);
            Assert.AreEqual("N5", settings?.Columns.First().Format);
            Assert.AreEqual("Decimal", settings?.Columns.First().TypeName);

            Assert.IsNotNull(settings?.ApiEndpoints);
            Assert.AreEqual(1, settings?.ApiEndpoints.Count);
            Assert.AreEqual(ApiEndpointType.Airlines, settings?.ApiEndpoints.First().EndpointType);
            Assert.AreEqual(ApiServiceType.AirLabs, settings?.ApiEndpoints.First().Service);
            Assert.AreEqual("https://airlabs.co/api/v9/airlines", settings?.ApiEndpoints.First().Url);

            Assert.IsNotNull(settings?.ApiServiceKeys);
            Assert.AreEqual(1, settings?.ApiServiceKeys.Count);
            Assert.AreEqual(ApiServiceType.AirLabs, settings?.ApiServiceKeys.First().Service);
            Assert.AreEqual("my-key", settings?.ApiServiceKeys.First().Key);
        }
    }
}
