using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Configuration;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class SimulatorSettingsBuilderTest
    {
        private ISimulatorSettingsBuilder _builder = null;
        private ICommandLineParser _parser = null;

        [TestInitialize]
        public void Initialise()
        {
            _builder = new SimulatorSettingsBuilder();
            _parser = new SimulatorCommandLineParser(null);
        }

        [TestMethod]
        public void DefaultConfigTest()
        {
            _parser!.Parse([]);
            var settings = _builder!.BuildSettings(_parser, "simulatorsettings.json");

            Assert.AreEqual(30003, settings?.Port);
            Assert.AreEqual(100, settings?.SendInterval);
            Assert.AreEqual(10, settings?.NumberOfAircraft);
            Assert.AreEqual(60000, settings?.MinimumAircraftLifespan);
            Assert.AreEqual(300000, settings?.MaximumAircraftLifespan);
            Assert.AreEqual("ReceiverSimulator.log", settings?.LogFile);
            Assert.AreEqual(Severity.Info, settings?.MinimumLogLevel);
        }

        [TestMethod]
        public void OverridePortTest()
        {
            var args = new string[] { "--port", "12345" };
            _parser!.Parse(args);
            var settings = _builder!.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(12345, settings?.Port);
        }

        [TestMethod]
        public void OverrideSendIntervalTest()
        {
            var args = new string[] { "--send-interval", "33456" };
            _parser!.Parse(args);
            var settings = _builder!.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(33456, settings?.SendInterval);
        }

        [TestMethod]
        public void OverrideNumberOfAircraftTest()
        {
            var args = new string[] { "--number", "126" };
            _parser!.Parse(args);
            var settings = _builder!.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(126, settings?.NumberOfAircraft);
        }

        [TestMethod]
        public void OverrideMinimumAircraftLifespanTest()
        {
            var args = new string[] { "--min-lifespan", "543" };
            _parser!.Parse(args);
            var settings = _builder!.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(543, settings?.MinimumAircraftLifespan);
        }

        [TestMethod]
        public void OverrideMaximumAircraftLifespanTest()
        {
            var args = new string[] { "--max-lifespan", "213234" };
            _parser!.Parse(args);
            var settings = _builder!.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(213234, settings?.MaximumAircraftLifespan);
        }

        [TestMethod]
        public void OverrideLogFileTest()
        {
            var args = new string[] { "--log-file", "MyLog.log" };
            _parser!.Parse(args);
            var settings = _builder!.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual("MyLog.log", settings?.LogFile);
        }

        [TestMethod]
        public void OverrideMinimumLogLevelTest()
        {
            var args = new string[] { "--log-level", "Debug" };
            _parser!.Parse(args);
            var settings = _builder!.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(Severity.Debug, settings?.MinimumLogLevel);
        }
    }
}
