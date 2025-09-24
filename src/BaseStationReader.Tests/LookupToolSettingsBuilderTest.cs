using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Tests
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
            Assert.AreEqual(ApiServiceType.AirLabs, settings.ApiServiceKeys[0].Service);
            Assert.AreEqual("51.47", settings.ReceiverLatitude?.ToString("#.##"));
            Assert.AreEqual("-.45", settings.ReceiverLongitude?.ToString("#.##"));

            var airlinesEndpoint = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Airlines);
            Assert.AreEqual(ApiServiceType.AirLabs, airlinesEndpoint.Service);
            Assert.AreEqual("https://airlabs.co/api/v9/airlines", airlinesEndpoint.Url);

            var aircraftEndpoint = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Aircraft);
            Assert.AreEqual(ApiServiceType.AirLabs, aircraftEndpoint.Service);
            Assert.AreEqual("https://airlabs.co/api/v9/fleets", aircraftEndpoint.Url);

            var flightsEndpoint = settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.ActiveFlights);
            Assert.AreEqual(ApiServiceType.AirLabs, flightsEndpoint.Service);
            Assert.AreEqual("https://airlabs.co/api/v9/flights", flightsEndpoint.Url);
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
