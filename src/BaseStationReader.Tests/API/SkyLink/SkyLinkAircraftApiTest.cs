using BaseStationReader.Entities.Api;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Api.SkyLink;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;

namespace BaseStationReader.Tests.API.SkyLink
{
    [TestClass]
    public class SkyLinkAircraftApiTest
    {
        private const string Address = "485785";
        private const string Registration = "PH-BHN";
        private const string ModelICAO = "B789";
        private const string Callsign = "KLM701";
        private const string Response = "{ \"aircraft\": [ { \"icao24\": \"485785\", \"callsign\": \"KLM701\", \"latitude\": 51.453003, \"longitude\": -1.185181, \"altitude\": 31975.0, \"ground_speed\": 451.677979, \"track\": 258.117859, \"vertical_rate\": 0.0, \"is_on_ground\": false, \"last_seen\": \"2025-10-02T19:45:46.299839\", \"first_seen\": \"2025-09-29T08:37:58.880856\", \"registration\": \"PH-BHN\", \"aircraft_type\": \"B789\", \"airline\": \"KLM\" } ], \"total_count\": 1, \"timestamp\": \"2025-10-02T19:45:50.299652\" }";

        private MockTrackerHttpClient _client = null;
        private IAircraftApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService()
                {
                    Service = ApiServiceType.SkyLink, Key = "an-api-key",
                    ApiEndpoints = [
                        new ApiEndpoint() { EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"}
                    ]
                }
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            var factory = new DatabaseManagementFactory(logger, context, 0, 0);
            _client = new MockTrackerHttpClient();
            _api = new SkyLinkAircraftApi(_client, factory, _settings);
        }

        [TestMethod]
        public async Task GetAircraftByAddressTestAsync()
        {
            _client.AddResponse(Response);
            var properties = await _api.LookupAircraftAsync(Address);

            Assert.IsNotNull(properties);
            Assert.HasCount(7, properties);
            Assert.AreEqual(Registration, properties[ApiProperty.AircraftRegistration]);
            Assert.IsEmpty(properties[ApiProperty.AircraftManufactured]);
            Assert.AreEqual(ModelICAO, properties[ApiProperty.ModelICAO]);
            Assert.IsEmpty(properties[ApiProperty.ModelName]);
            Assert.IsEmpty(properties[ApiProperty.ManufacturerName]);
            Assert.IsEmpty(properties[ApiProperty.ManufacturerName]);
            Assert.AreEqual(Callsign, properties[ApiProperty.Callsign]);
        }

        [TestMethod]
        public async Task EmptyResponseTestAsync()
        {
            _client.AddResponse("{\"aircraft\": []}");
            var properties = await _api.LookupAircraftAsync(Address);

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task NullAircraftTestAsync()
        {
            _client.AddResponse("{\"aircraft\": [ null ]}");
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
    }
}
