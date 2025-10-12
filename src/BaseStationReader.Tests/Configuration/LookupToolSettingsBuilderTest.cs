using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Config;

namespace BaseStationReader.Tests.Configuration
{
    [TestClass]
    public class LookupToolSettingsBuilderTest
    {
        private ILookupToolSettingsBuilder _builder = null;
        private ICommandLineParser _parser = null;

        [TestInitialize]
        public void Initialise()
        {
            _builder = new LookupToolSettingsBuilder();
            _parser = new LookupToolCommandLineParser(null);
        }

        [TestMethod]
        public void DefaultConfigTest()
        {
            _parser.Parse([]);
            var settings = _builder.BuildSettings(_parser, "lookupsettings.json");

            Assert.AreEqual("AircraftLookup.log", settings.LogFile);
            Assert.AreEqual(Severity.Info, settings.MinimumLogLevel);
            Assert.IsFalse(settings.CreateSightings);
            Assert.AreEqual("AirLabs", settings.LiveApi);
            Assert.AreEqual("AeroDataBox", settings.HistoricalApi);
            Assert.AreEqual("CheckWXApi", settings.WeatherApi);
            Assert.AreEqual("51.47", settings.ReceiverLatitude?.ToString("#.##"));
            Assert.AreEqual("-.45", settings.ReceiverLongitude?.ToString("#.##"));
            Assert.AreEqual("09:00", settings.ScheduleStartTime);
            Assert.AreEqual("21:00", settings.ScheduleEndTime);

            var airlinesEndpoint = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Airlines && x.Service == ApiServiceType.AirLabs);
            Assert.AreEqual(ApiServiceType.AirLabs, airlinesEndpoint.Service);
            Assert.AreEqual("https://airlabs.co/api/v9/airlines", airlinesEndpoint.Url);

            var aircraftEndpoint = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Aircraft && x.Service == ApiServiceType.AirLabs);
            Assert.AreEqual(ApiServiceType.AirLabs, aircraftEndpoint.Service);
            Assert.AreEqual("https://airlabs.co/api/v9/fleets", aircraftEndpoint.Url);

            var flightsEndpoint = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.ActiveFlights && x.Service == ApiServiceType.AirLabs);
            Assert.AreEqual(ApiServiceType.AirLabs, flightsEndpoint.Service);
            Assert.AreEqual("https://airlabs.co/api/v9/flights", flightsEndpoint.Url);

            aircraftEndpoint = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Aircraft && x.Service == ApiServiceType.AeroDataBox);
            Assert.AreEqual(ApiServiceType.AeroDataBox, aircraftEndpoint.Service);
            Assert.AreEqual("https://aerodatabox.p.rapidapi.com/aircrafts", aircraftEndpoint.Url);

            flightsEndpoint = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.HistoricalFlights && x.Service == ApiServiceType.AeroDataBox);
            Assert.AreEqual(ApiServiceType.AeroDataBox, flightsEndpoint.Service);
            Assert.AreEqual("https://aerodatabox.p.rapidapi.com/flights", flightsEndpoint.Url);
        }

        [TestMethod]
        public void OverrideLogFileTest()
        {
            var args = new string[] { "--log-file", "MyLog.log" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "trackersettings.json");
            Assert.AreEqual("MyLog.log", settings.LogFile);
        }

        [TestMethod]
        public void OverrideMinimumLogLevelTest()
        {
            var args = new string[] { "--log-level", "Debug" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "trackersettings.json");
            Assert.AreEqual(Severity.Debug, settings.MinimumLogLevel);
        }

        [TestMethod]
        public void OverrideCreateSettingsTest()
        {
            var args = new string[] { "--create-sightings", "true" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "lookupsettings.json");
            Assert.IsTrue(settings.CreateSightings);
        }

        [TestMethod]
        public void OverrideLiveApiTest()
        {
            var args = new string[] { "--live-api", "Missing" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "lookupsettings.json");
            Assert.AreEqual("Missing", settings.LiveApi);
        }

        [TestMethod]
        public void OverrideHistoricalApiTest()
        {
            var args = new string[] { "--historical-api", "Missing" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "lookupsettings.json");
            Assert.AreEqual("Missing", settings.HistoricalApi);
        }

        [TestMethod]
        public void OverrideWeatherApiTest()
        {
            var args = new string[] { "--weather-api", "Missing" };
            _parser.Parse(args);
            var settings = _builder.BuildSettings(_parser, "lookupsettings.json");
            Assert.AreEqual("Missing", settings.WeatherApi);
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
