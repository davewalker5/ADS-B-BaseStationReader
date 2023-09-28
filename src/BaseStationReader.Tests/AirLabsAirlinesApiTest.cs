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
    public class AirLabsAirlinesApiTest
    {
        private const string Response = "{\"response\": [{\"name\": \"Jet2.com\", \"iata_code\": \"LS\", \"icao_code\": \"EXS\"}]}";

        private MockTrackerHttpClient? _client = null;
        private IAirlinesApi? _api = null;

        [TestInitialize]
        public void Initialise()
        {
            _client = new MockTrackerHttpClient();
            _api = new AirLabsAirlinesApi(_client, "", "");
        }

        [TestMethod]
        public void GetAirlineByIATACodeTest()
        {
            _client!.AddResponse(Response);
            var properties = Task.Run(() => _api!.LookupAirlineByIATACode("LS")).Result;

            Assert.IsNotNull(properties);
            Assert.AreEqual(3, properties.Count);
            Assert.AreEqual("LS", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("EXS", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("Jet2.com", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public void GetAirlineByICAOCodeTest()
        {
            _client!.AddResponse(Response);
            var properties = Task.Run(() => _api!.LookupAirlineByICAOCode("EXS")).Result;

            Assert.IsNotNull(properties);
            Assert.AreEqual(3, properties.Count);
            Assert.AreEqual("LS", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("EXS", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("Jet2.com", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client!.AddResponse("{}");
            var properties = Task.Run(() => _api!.LookupAirlineByICAOCode("EXS")).Result;

            Assert.IsNull(properties);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client!.AddResponse(null);
            var properties = Task.Run(() => _api!.LookupAirlineByICAOCode("EXS")).Result;

            Assert.IsNull(properties);
        }
    }
}
