using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Interfaces.Config;

namespace BaseStationReader.Tests.Configuration
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
            _parser.Parse([]);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");

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
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(12345, settings?.Port);
        }

        [TestMethod]
        public void OverrideSendIntervalTest()
        {
            var args = new string[] { "--send-interval", "33456" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(33456, settings?.SendInterval);
        }

        [TestMethod]
        public void OverrideNumberOfAircraftTest()
        {
            var args = new string[] { "--number", "126" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(126, settings?.NumberOfAircraft);
        }

        [TestMethod]
        public void OverrideMinimumAircraftLifespanTest()
        {
            var args = new string[] { "--min-lifespan", "543" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(543, settings?.MinimumAircraftLifespan);
        }

        [TestMethod]
        public void OverrideMaximumAircraftLifespanTest()
        {
            var args = new string[] { "--max-lifespan", "213234" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(213234, settings?.MaximumAircraftLifespan);
        }

        [TestMethod]
        public void OverrideLogFileTest()
        {
            var args = new string[] { "--log-file", "MyLog.log" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual("MyLog.log", settings?.LogFile);
        }

        [TestMethod]
        public void OverrideMinimumLogLevelTest()
        {
            var args = new string[] { "--log-level", "Debug" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(Severity.Debug, settings?.MinimumLogLevel);
        }

        [TestMethod]
        public void OverrideMinimumAltitudeTest()
        {
            var args = new string[] { "--min-altitude", "25178" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(25178, settings?.MinimumAltitude);
        }

        [TestMethod]
        public void OverrideMaximumAltitudeTest()
        {
            var args = new string[] { "--max-altitude", "589" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(589, settings?.MaximumAltitude);
        }

        [TestMethod]
        public void OverrideMinimumTakeoffSpeedTest()
        {
            var args = new string[] { "--min-takeoffspeed", "26" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(26, settings?.MinimumTakeOffSpeed);
        }

        [TestMethod]
        public void OverrideMaximumTakeoffSpeedTest()
        {
            var args = new string[] { "--max-takeoffspeed", "986" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(986, settings?.MaximumTakeOffSpeed);
        }

        [TestMethod]
        public void OverrideMinimumApproachSpeedTest()
        {
            var args = new string[] { "--min-approachspeed", "154" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(154, settings?.MinimumApproachSpeed);
        }

        [TestMethod]
        public void OverrideMaximumApproachSpeedTest()
        {
            var args = new string[] { "--max-approachspeed", "284" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(284, settings?.MaximumApproachSpeed);
        }

        [TestMethod]
        public void OverrideMinimumCruisingSpeedTest()
        {
            var args = new string[] { "--min-cruisespeed", "387" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(387, settings?.MinimumCruisingSpeed);
        }

        [TestMethod]
        public void OverrideMaximumCruisingSpeedTest()
        {
            var args = new string[] { "--max-cruisespeed", "476" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(476, settings?.MaximumCruisingSpeed);
        }

        [TestMethod]
        public void OverrideMinimumClimbRateTest()
        {
            var args = new string[] { "--min-climbrate", "153" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(153, settings?.MinimumClimbRate);
        }

        [TestMethod]
        public void OverrideMaximumClimbRateTest()
        {
            var args = new string[] { "--max-climbrate", "1054" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(1054, settings?.MaximumClimbRate);
        }

        [TestMethod]
        public void OverrideMinimumDescentRateTest()
        {
            var args = new string[] { "--min-descentrate", "75" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(75, settings?.MinimumDescentRate);
        }

        [TestMethod]
        public void OverrideMaximumDescentRateTest()
        {
            var args = new string[] { "--max-descentrate", "2013" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(2013, settings?.MaximumDescentRate);
        }

        [TestMethod]
        public void OverrideMaximumInitialTest()
        {
            var args = new string[] { "--max-range", "62" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "simulatorsettings.json");
            Assert.AreEqual(62, settings?.MaximumInitialRange);
        }

        [TestMethod]
        public void OverrideReceiverLatitudeTest()
        {
            var args = new string[] { "--latitude", "58.93" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "trackersettings.json");
            Assert.AreEqual(58.93, Math.Round((double)settings.ReceiverLatitude, 2, MidpointRounding.AwayFromZero));
        }

        [TestMethod]
        public void OverrideReceiverLongitueTest()
        {
            var args = new string[] { "--longitude", "120.56" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "trackersettings.json");
            Assert.AreEqual(120.56, Math.Round((double)settings.ReceiverLongitude, 2, MidpointRounding.AwayFromZero));
        }
    }
}
