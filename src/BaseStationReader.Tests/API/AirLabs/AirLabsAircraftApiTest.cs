using BaseStationReader.Entities.Api;
using BaseStationReader.Api.AirLabs;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;

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
        private const string ResponseWithNoBuildDate = "{ \"response\": [ { \"hex\": \"4076ED\", \"reg_number\": \"G-DRTO\", \"flag\": \"UK\", \"airline_icao\": \"EXS\", \"airline_iata\": \"LS\", \"seen\": 4902263, \"icao\": \"B738\", \"iata\": \"73H\", \"model\": \"Boeing 737-800 (winglets) pax\", \"engine\": \"jet\", \"engine_count\": \"2\", \"manufacturer\": \"BOEING\", \"type\": \"landplane\", \"category\": \"M\", \"msn\": null, \"line\": null, \"lat\": 27.93442, \"lng\": -15.38821, \"alt\": null, \"dir\": 288, \"speed\": null, \"v_speed\": null, \"squawk\": null, \"last_seen\": \"2025-09-18 17:31:58\" } ] }";
        private const string ResponseWithNullBuildDate = "{ \"response\": [ { \"hex\": \"4076ED\", \"reg_number\": \"G-DRTO\", \"flag\": \"UK\", \"airline_icao\": \"EXS\", \"airline_iata\": \"LS\", \"seen\": 4902263, \"icao\": \"B738\", \"iata\": \"73H\", \"model\": \"Boeing 737-800 (winglets) pax\", \"engine\": \"jet\", \"engine_count\": \"2\", \"manufacturer\": \"BOEING\", \"type\": \"landplane\", \"category\": \"M\", \"built\": null, \"msn\": null, \"line\": null, \"lat\": 27.93442, \"lng\": -15.38821, \"alt\": null, \"dir\": 288, \"speed\": null, \"v_speed\": null, \"squawk\": null, \"last_seen\": \"2025-09-18 17:31:58\" } ] }";
        private const string ResponseWithNoRegistration = "{ \"response\": [ { \"hex\": \"4076ED\", \"flag\": \"UK\", \"airline_icao\": \"EXS\", \"airline_iata\": \"LS\", \"seen\": 4902263, \"icao\": \"B738\", \"iata\": \"73H\", \"model\": \"Boeing 737-800 (winglets) pax\", \"engine\": \"jet\", \"engine_count\": \"2\", \"manufacturer\": \"BOEING\", \"type\": \"landplane\", \"category\": \"M\", \"built\": 2011, \"age\": 10, \"msn\": null, \"line\": null, \"lat\": 27.93442, \"lng\": -15.38821, \"alt\": null, \"dir\": 288, \"speed\": null, \"v_speed\": null, \"squawk\": null, \"last_seen\": \"2025-09-18 17:31:58\" } ] }";

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
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            var factory = new DatabaseManagementFactory(logger, context, 0, 0);
            _client = new MockTrackerHttpClient();
            _api = new AirLabsAircraftApi(_client, factory, _settings);
        }

        [TestMethod]
        public async Task GetAircraftByAddressTestAsync()
        {
            _client.AddResponse(Response);
            var expectedAge = (DateTime.Today.Year - int.Parse(Manufactured)).ToString();
            var properties = await _api.LookupAircraftAsync(Address);

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
        public async Task GetAircraftWithNoBuildDateByAddressTestAsync()
        {
            _client.AddResponse(ResponseWithNoBuildDate);
            var properties = await _api.LookupAircraftAsync(Address);

            Assert.IsNotNull(properties);
            Assert.HasCount(7, properties);
            Assert.AreEqual(Registration, properties[ApiProperty.AircraftRegistration]);
            Assert.IsEmpty(properties[ApiProperty.AircraftManufactured]);
            Assert.IsEmpty(properties[ApiProperty.AircraftAge]);
            Assert.AreEqual(Manufacturer, properties[ApiProperty.ManufacturerName]);
            Assert.AreEqual(ModelICAO, properties[ApiProperty.ModelICAO]);
            Assert.AreEqual(ModelIATA, properties[ApiProperty.ModelIATA]);
            Assert.AreEqual(ModelName, properties[ApiProperty.ModelName]);
        }

        [TestMethod]
        public async Task GetAircraftWithNullBuildDateByAddressTestAsync()
        {
            _client.AddResponse(ResponseWithNullBuildDate);
            var properties = await _api.LookupAircraftAsync(Address);

            Assert.IsNotNull(properties);
            Assert.HasCount(7, properties);
            Assert.AreEqual(Registration, properties[ApiProperty.AircraftRegistration]);
            Assert.IsEmpty(properties[ApiProperty.AircraftManufactured]);
            Assert.IsEmpty(properties[ApiProperty.AircraftAge]);
            Assert.AreEqual(Manufacturer, properties[ApiProperty.ManufacturerName]);
            Assert.AreEqual(ModelICAO, properties[ApiProperty.ModelICAO]);
            Assert.AreEqual(ModelIATA, properties[ApiProperty.ModelIATA]);
            Assert.AreEqual(ModelName, properties[ApiProperty.ModelName]);
        }

        [TestMethod]
        public async Task GetAircraftWithNoRegistrationByAddressTestAsync()
        {
            _client.AddResponse(ResponseWithNoRegistration);
            var properties = await _api.LookupAircraftAsync(Address);

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task NullResponseTestAsync()
        {
            _client.AddResponse(null);
            var properties = await _api.LookupAircraftAsync(Address);

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task InvalidJsonResponseTestAsync()
        {
            _client.AddResponse("{}");
            var properties = await _api.LookupAircraftAsync(Address);

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task EmptyJsonResponseTestAsync()
        {
            _client.AddResponse("{\"response\": []}");
            var properties = await _api.LookupAircraftAsync(Address);

            Assert.IsNull(properties);
        }
    }
}
