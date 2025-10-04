using BaseStationReader.Entities.Api;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Tests.API.AirLabs
{
    [TestClass]
    public class AirLabsAircraftApiTest
    {
        private const string Address = "4076ED";
        private const string Registration = "G-DRTO";
        private const string Manufactured = "2011";
        private const string Manufacturer = "BOEING";
        private const string ModelICAO = "B738";
        private const string ModelIATA = "73H";
        private const string ModelName = "Boeing 737-800 (winglets) pax";
        private const string Response = "{ \"response\": [ { \"hex\": \"4076ED\", \"reg_number\": \"G-DRTO\", \"flag\": \"UK\", \"airline_icao\": \"EXS\", \"airline_iata\": \"LS\", \"seen\": 4902263, \"icao\": \"B738\", \"iata\": \"73H\", \"model\": \"Boeing 737-800 (winglets) pax\", \"engine\": \"jet\", \"engine_count\": \"2\", \"manufacturer\": \"BOEING\", \"type\": \"landplane\", \"category\": \"M\", \"built\": 2011, \"age\": 10, \"msn\": null, \"line\": null, \"lat\": 27.93442, \"lng\": -15.38821, \"alt\": null, \"dir\": 288, \"speed\": null, \"v_speed\": null, \"squawk\": null, \"last_seen\": \"2025-09-18 17:31:58\" } ] }";

        private MockTrackerHttpClient _client = null;
        private IAircraftApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.AirLabs, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _api = new AirLabsAircraftApi(logger, _client, null, _settings);
        }

        [TestMethod]
        public void GetAircraftByAddressTest()
        {
            _client.AddResponse(Response);
            var expectedAge = (DateTime.Today.Year - int.Parse(Manufactured)).ToString();
            var properties = Task.Run(() => _api.LookupAircraftAsync(Address)).Result;

            Assert.IsNotNull(properties);
            Assert.HasCount(7, properties);
            Assert.AreEqual(Registration, properties[ApiProperty.AircraftRegistration]);
            Assert.AreEqual(Manufactured, properties[ApiProperty.AircraftManufactured]);
            Assert.AreEqual(expectedAge, properties[ApiProperty.AircraftAge]);
            Assert.AreEqual(Manufacturer, properties[ApiProperty.ManufacturerName]);
            Assert.AreEqual(ModelICAO, properties[ApiProperty.ModelICAO]);
            Assert.AreEqual(ModelIATA, properties[ApiProperty.ModelIATA]);
            Assert.AreEqual(ModelName, properties[ApiProperty.ModelName]);
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client.AddResponse("{}");
            var properties = Task.Run(() => _api.LookupAircraftAsync(Address)).Result;

            Assert.IsNull(properties);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client.AddResponse(null);
            var properties = Task.Run(() => _api.LookupAircraftAsync(Address)).Result;

            Assert.IsNull(properties);
        }
    }
}
