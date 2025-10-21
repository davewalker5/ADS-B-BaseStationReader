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
    public class AirLabsAirlinesApiTest
    {
        private const string Response = "{\"response\": [{\"name\": \"Jet2.com\", \"iata_code\": \"LS\", \"icao_code\": \"EXS\"}]}";
        private const string NoIATACode = "{\"response\": [{\"name\": \"Jet2.com\", \"iata_code\": null, \"icao_code\": \"EXS\"}]}";
        private const string NoICAOCode = "{\"response\": [{\"name\": \"Jet2.com\", \"iata_code\": \"LS\", \"icao_code\": null}]}";

        private MockTrackerHttpClient _client = null;
        private IAirlinesApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.AirLabs, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.Airlines, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            var factory = new DatabaseManagementFactory(logger, context, 0, 0);
            _client = new MockTrackerHttpClient();
            _api = new AirLabsAirlinesApi(_client, factory, _settings);
        }

        [TestMethod]
        public async Task GetAirlineByIATACodeTestAsync()
        {
            _client.AddResponse(Response);
            var properties = await _api.LookupAirlineByIATACodeAsync("LS");

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("LS", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("EXS", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("Jet2.com", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public async Task GetAirlineByICAOCodeTestAsync()
        {
            _client.AddResponse(Response);
            var properties = await _api.LookupAirlineByICAOCodeAsync("EXS");

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("LS", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("EXS", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("Jet2.com", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public async Task NoIATACodeTestAsync()
        {
            _client.AddResponse(NoIATACode);
            var properties = await _api.LookupAirlineByICAOCodeAsync("EXS");

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("EXS", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("Jet2.com", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public async Task NoICAOCodeTestAsync()
        {
            _client.AddResponse(NoICAOCode);
            var properties = await _api.LookupAirlineByICAOCodeAsync("EXS");

            Assert.IsNotNull(properties);
            Assert.HasCount(3, properties);
            Assert.AreEqual("LS", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("", properties[ApiProperty.AirlineICAO]);
            Assert.AreEqual("Jet2.com", properties[ApiProperty.AirlineName]);
        }

        [TestMethod]
        public async Task NullResponseTestAsync()
        {
            _client.AddResponse(null);
            var properties = await _api.LookupAirlineByICAOCodeAsync("EXS");

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task InvalidJsonResponseTestAsync()
        {
            _client.AddResponse("{}");
            var properties = await _api.LookupAirlineByICAOCodeAsync("EXS");

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task EmptyJsonResponseTestAsync()
        {
            _client.AddResponse("{\"response\": []}");
            var properties = await _api.LookupAirlineByICAOCodeAsync("EXS");

            Assert.IsNull(properties);
        }
    }
}
