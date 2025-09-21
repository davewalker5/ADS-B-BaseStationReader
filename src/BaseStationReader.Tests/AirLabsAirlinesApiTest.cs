using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using BaseStationReader.BusinessLogic.Api.AirLabs;
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
        private const string NoIATACode = "{\"response\": [{\"name\": \"Jet2.com\", \"iata_code\": null, \"icao_code\": \"EXS\"}]}";
        private const string NoICAOCode = "{\"response\": [{\"name\": \"Jet2.com\", \"iata_code\": \"LS\", \"icao_code\": null}]}";

        private MockTrackerHttpClient _client = null;
        private IAirlinesApi _api = null;

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _api = new AirLabsAirlinesApi(logger, _client, "", "");
        }

        [TestMethod]
        public void GetAirlineByIATACodeTest()
        {
            _client!.AddResponse(Response);
            var properties = Task.Run(() => _api!.LookupAirlineByIATACodeAsync("LS")).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("LS", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("EXS", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("Jet2.com", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public void GetAirlineByICAOCodeTest()
        {
            _client!.AddResponse(Response);
            var properties = Task.Run(() => _api!.LookupAirlineByICAOCodeAsync("EXS")).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("LS", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("EXS", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("Jet2.com", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public void NoIATACodeTest()
        {
            _client!.AddResponse(NoIATACode);
            var properties = Task.Run(() => _api!.LookupAirlineByICAOCodeAsync("EXS")).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("EXS", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("Jet2.com", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public void NoICAOCodeTest()
        {
            _client!.AddResponse(NoICAOCode);
            var properties = Task.Run(() => _api!.LookupAirlineByICAOCodeAsync("EXS")).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("LS", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("Jet2.com", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client!.AddResponse("{}");
            var properties = Task.Run(() => _api!.LookupAirlineByICAOCodeAsync("EXS")).Result;

            Assert.IsNull(properties);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client!.AddResponse(null);
            var properties = Task.Run(() => _api!.LookupAirlineByICAOCodeAsync("EXS")).Result;

            Assert.IsNull(properties);
        }
    }
}
