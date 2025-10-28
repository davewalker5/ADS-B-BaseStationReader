using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API.Wrapper
{
    [TestClass]
    public class AirlineLookupManagerTest
    {
        private const string IATA = "BA";
        private const string ICAO = "BAW";
        private const string Name = "British Airways";
        private const string Response = "[ { \"id\": 1355, \"name\": \"British Airways\", \"alias\": null, \"iata\": \"BA\", \"icao\": \"BAW\", \"callsign\": \"SPEEDBIRD\", \"country\": \"United Kingdom\", \"active\": \"Y\", \"logo\": \"https://media.skylinkapi.com/logos/BA.png\" } ]";

        private IDatabaseManagementFactory _factory;
        private IAirlineLookupManager _manager;
        private MockTrackerHttpClient _client;
        private IExternalApi _api;
        private IExternalApiRegister _register;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService()
                {
                    Service = ApiServiceType.SkyLink, Key = "an-api-key",
                    ApiEndpoints = [
                        new ApiEndpoint() { EndpointType = ApiEndpointType.Airlines, Url = "http://some.host.com/endpoint"}
                    ]
                }
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            // Construct a database management factory
            var logger = new MockFileLogger();
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);

            // Construct the lookup management instance
            _client = new MockTrackerHttpClient();
            _api = new ExternalApiFactory().GetApiInstance(ApiServiceType.SkyLink, ApiEndpointType.Airlines, _client, _factory, _settings);
            _register = new ExternalApiRegister(logger);
            _manager = new AirlineLookupManager(_register, _factory);
        }

        [TestMethod]
        public async Task LookupWithInvalidPropertiesTestAsyc()
        {
            _register.RegisterExternalApi(ApiEndpointType.Airlines, _api);
            var airline = await _manager.IdentifyAirlineAsync(null, null, null);
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task LookupWithInvalidApiTestAsyc()
        {
            _client.AddResponse(Response);
            var airline = await _manager.IdentifyAirlineAsync(IATA, null, null);
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task LookupFromDatabaseUsingIATACodeTestAsyc()
        {
            _register.RegisterExternalApi(ApiEndpointType.Airlines, _api);
            var local = await _factory.AirlineManager.AddAsync(IATA, ICAO, Name);

            var airline = await _manager.IdentifyAirlineAsync(IATA, null, null);

            Assert.IsNotNull(airline);
            Assert.AreEqual(local.Id, airline.Id);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task LookupFromDatabaseUsingICAOCodeTestAsyc()
        {
            _register.RegisterExternalApi(ApiEndpointType.Airlines, _api);
            var local = await _factory.AirlineManager.AddAsync(IATA, ICAO, Name);

            var airline = await _manager.IdentifyAirlineAsync(null, ICAO, null);

            Assert.IsNotNull(airline);
            Assert.AreEqual(local.Id, airline.Id);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task LookupFromDatabaseUsingNameTestAsyc()
        {
            _register.RegisterExternalApi(ApiEndpointType.Airlines, _api);
            var local = await _factory.AirlineManager.AddAsync(IATA, ICAO, Name);

            var airline = await _manager.IdentifyAirlineAsync(null, null, Name);

            Assert.IsNotNull(airline);
            Assert.AreEqual(local.Id, airline.Id);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task LookupViaApiUsingIATACodeTestAsync()
        {
            _register.RegisterExternalApi(ApiEndpointType.Airlines, _api);
            _client.AddResponse(Response);

            var airline = await _manager.IdentifyAirlineAsync(IATA, null, null);

            Assert.IsNotNull(airline);
            Assert.IsGreaterThan(0, airline.Id);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }

        [TestMethod]
        public async Task LookupViaApiUsingICAOCodeTestAsync()
        {
            _register.RegisterExternalApi(ApiEndpointType.Airlines, _api);
            _client.AddResponse(Response);

            var airline = await _manager.IdentifyAirlineAsync(null, ICAO, null);

            Assert.IsNotNull(airline);
            Assert.IsGreaterThan(0, airline.Id);
            Assert.AreEqual(IATA, airline.IATA);
            Assert.AreEqual(ICAO, airline.ICAO);
            Assert.AreEqual(Name, airline.Name);
        }
    }
}