using BaseStationReader.Entities.Lookup;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.BusinessLogic.Api.AeroDatabox;
using BaseStationReader.Interfaces.Api;

namespace BaseStationReader.Tests.API.AeroDataBox
{
    /// <summary>
    /// These tests can't test authentication/authorisation at the service end, the lookup of data at the
    /// service end or network transport. They're design to test the downstream logic once a response has
    /// been received
    /// </summary>
    [TestClass]
    public class AeroDataBoxAircraftApiTest
    {
        private const string Address = "407F94";
        private const string Response = "{ \"id\": 6565, \"reg\": \"G-DHLR\", \"active\": true, \"serial\": \"41748\", \"hexIcao\": \"407F94\", \"airlineName\": \"DHL Air\", \"iataCodeShort\": \"763\", \"icaoCode\": \"B763\", \"model\": \"B763\", \"modelCode\": \"767-316ERBCF\", \"numSeats\": 221, \"rolloutDate\": \"1991-07-26\", \"firstFlightDate\": \"2012-09-17\", \"deliveryDate\": \"2012-09-26\", \"registrationDate\": \"2023-09-04\", \"typeName\": \"Boeing 767-300\", \"numEngines\": 2, \"engineType\": \"Jet\", \"isFreighter\": true, \"productionLine\": \"Boeing 767\", \"ageYears\": 34.2, \"verified\": true, \"numRegistrations\": 4 }";

        private MockTrackerHttpClient _client = null;
        private IAircraftApi _api = null;

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _api = new AeroDataBoxAircraftApi(logger, _client, "https://https://some.host.com", "");
        }

        [TestMethod]
        public void GetAircraftByAddressTest()
        {
            _client.AddResponse(Response);
            var properties = Task.Run(() => _api.LookupAircraftAsync(Address)).Result;

            var expectedAge = (DateTime.Now.Year - 2012).ToString();
            Assert.IsNotNull(properties);
            Assert.HasCount(7, properties);
            Assert.AreEqual("G-DHLR", properties[ApiProperty.AircraftRegistration]);
            Assert.AreEqual("2012", properties[ApiProperty.AircraftManufactured]);
            Assert.AreEqual(expectedAge, properties[ApiProperty.AircraftAge]);
            Assert.AreEqual("B763", properties[ApiProperty.ModelICAO]);
            Assert.AreEqual("763", properties[ApiProperty.ModelIATA]);
            Assert.AreEqual("Boeing 767-300", properties[ApiProperty.ModelName]);
            Assert.IsEmpty(properties[ApiProperty.ManufacturerName]);
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
