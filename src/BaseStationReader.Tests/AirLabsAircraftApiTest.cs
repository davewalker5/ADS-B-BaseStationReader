using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic.Api.AirLabs;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests
{
    /// <summary>
    /// These tests can't test authentication/authorisation at the service end, the lookup of data at the
    /// service end or network transport. They're design to test the downstream logic once a response has
    /// been received
    /// </summary>
    [TestClass]
    public class AirLabsAircraftApiTest
    {
        private const string Response = "{\"response\": [ { \"hex\": \"4B012F\", \"airline_icao\": \"IMX\", \"airline_iata\": \"XM\", \"manufacturer\": \"ATR\", \"icao\": \"AT75\", \"iata\": null }]}";

        private MockTrackerHttpClient _client = null;
        private IAircraftApi _api = null;

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _api = new AirLabsAircraftApi(logger, _client, "", "");
        }

        [TestMethod]
        public void GetAircraftByAddressTest()
        {
            _client!.AddResponse(Response);
            var properties = Task.Run(() => _api!.LookupAircraft("4B012F")).Result;

            Assert.IsNotNull(properties);
            Assert.AreEqual(5, properties.Count);
            Assert.AreEqual("XM", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("IMX", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("ATR", properties[ApiProperty.ManufacturerName]);
            Assert.AreEqual("", properties[ApiProperty.ModelIATA]);
            Assert.AreEqual("AT75", properties[ApiProperty.ModelICAO]);
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client!.AddResponse("{}");
            var properties = Task.Run(() => _api!.LookupAircraft("4B012F")).Result;

            Assert.IsNull(properties);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client!.AddResponse(null);
            var properties = Task.Run(() => _api!.LookupAircraft("4B012F")).Result;

            Assert.IsNull(properties);
        }
    }
}
