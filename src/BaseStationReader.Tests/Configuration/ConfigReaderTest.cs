using BaseStationReader.BusinessLogic.Configuration;

namespace BaseStationReader.Tests.Configuration
{
    [TestClass]
    public class ConfigReaderTest : TrackerSettingsTestBase
    {
        [TestMethod]
        public void ReadAppSettingsTest()
        {
            var settings = new TrackerConfigReader().Read("trackersettings.json");
            AssertCorrectSettings(settings);
        }
    }
}
