using BaseStationReader.BusinessLogic.Configuration;

namespace BaseStationReader.Tests
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
