using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using System.Security.Cryptography.X509Certificates;

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
            Assert.AreEqual(ApiServiceType.AirLabs, settings.ApiServiceKeys[0].Service);

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
    }
}
