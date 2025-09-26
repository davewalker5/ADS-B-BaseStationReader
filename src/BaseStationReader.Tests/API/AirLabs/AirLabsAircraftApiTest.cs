using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API.AirLabs
{
    /// <summary>
    /// These tests can't test authentication/authorisation at the service end, the lookup of data at the
    /// service end or network transport. They're design to test the downstream logic once a response has
    /// been received
    /// </summary>
    [TestClass]
    public class AirLabsAircraftApiTest
    {
        private const string Address = "4076ED";
        private const string Response = "{ \"response\": [ { \"hex\": \"4076ED\", \"reg_number\": \"G-DRTO\", \"flag\": \"UK\", \"airline_icao\": \"EXS\", \"airline_iata\": \"LS\", \"seen\": 4902263, \"icao\": \"B738\", \"iata\": \"73H\", \"model\": \"Boeing 737-800 (winglets) pax\", \"engine\": \"jet\", \"engine_count\": \"2\", \"manufacturer\": \"BOEING\", \"type\": \"landplane\", \"category\": \"M\", \"built\": 2011, \"age\": 10, \"msn\": null, \"line\": null, \"lat\": 27.93442, \"lng\": -15.38821, \"alt\": null, \"dir\": 288, \"speed\": null, \"v_speed\": null, \"squawk\": null, \"last_seen\": \"2025-09-18 17:31:58\" } ] }";

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
            _client.AddResponse(Response);
            var properties = Task.Run(() => _api.LookupAircraftAsync(Address)).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(7, properties);
            Assert.AreEqual("G-DRTO", properties[ApiProperty.AircraftRegistration]);
            Assert.AreEqual("2011", properties[ApiProperty.AircraftManufactured]);
            Assert.AreEqual("10", properties[ApiProperty.AircraftAge]);
            Assert.AreEqual("BOEING", properties[ApiProperty.ManufacturerName]);
            Assert.AreEqual("B738", properties[ApiProperty.ModelICAO]);
            Assert.AreEqual("73H", properties[ApiProperty.ModelIATA]);
            Assert.AreEqual("Boeing 737-800 (winglets) pax", properties[ApiProperty.ModelName]);
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client.AddResponse("{}");
            var properties = Task.Run(() => _api!.LookupAircraftAsync(Address)).Result;

            Assert.IsNull(properties);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client.AddResponse(null);
            var properties = Task.Run(() => _api!.LookupAircraftAsync(Address)).Result;

            Assert.IsNull(properties);
        }
    }
}
