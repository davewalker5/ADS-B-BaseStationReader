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
    public class AircraftLookupManagerTest
    {
        private const string Address = "8963E6";
        private const string Manufacturer = "Airbus";
        private const string ModelIATA = "388";
        private const string ModelICAO = "A388";
        private const string ModelName = "Airbus A380-800";
        private const string Registration = "A6-EOI";
        private const string Response = "{ \"aircraft\": [ { \"icao24\": \"8963E6\", \"callsign\": \"UAE7CN\", \"latitude\": 51.389191, \"longitude\": -0.480652, \"altitude\": 4275.0, \"ground_speed\": 272.23703, \"track\": 134.851181, \"vertical_rate\": 960.0, \"is_on_ground\": false, \"last_seen\": \"2025-10-27T09:44:16.959156\", \"first_seen\": \"2025-10-26T17:24:40.329980\", \"registration\": \"A6-EOI\", \"aircraft_type\": \"A388\", \"airline\": null } ], \"total_count\": 1, \"timestamp\": \"2025-10-27T09:44:21.079513\" }";

        private IDatabaseManagementFactory _factory;
        private IAircraftLookupManager _manager;
        private MockTrackerHttpClient _client;
        private Model _model;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.SkyLink, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public async Task Initialise()
        {
            // Construct a database management factory
            var logger = new MockFileLogger();
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);

            // Add the model and manufacturer
            var manufacturer = await _factory.ManufacturerManager.AddAsync(Manufacturer);
            _model = await _factory.ModelManager.AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);

            // Construct the lookup management instance
            _client = new MockTrackerHttpClient();
            var api = new ExternalApiFactory().GetApiInstance(ApiServiceType.SkyLink, ApiEndpointType.Aircraft, _client, _factory, _settings);
            var register = new ExternalApiRegister(logger);
            register.RegisterExternalApi(ApiEndpointType.Aircraft, api);
            _manager = new AircraftLookupManager(register, _factory);
        }

        [TestMethod]
        public async Task LookupFromDatabaseTestAsyc()
        {
            var local = await _factory.AircraftManager.AddAsync(Address, Registration, null, null, _model.Id);

            var aircraft = await _manager.IdentifyAircraftAsync(Address);

            Assert.IsNotNull(aircraft);
            Assert.AreEqual(local.Id, aircraft.Id);
            Assert.AreEqual(Address, aircraft.Address);
            Assert.AreEqual(Registration, aircraft.Registration);
            Assert.IsNull(aircraft.Manufactured);
            Assert.IsNull(aircraft.Age);
            Assert.AreEqual(ModelIATA, aircraft.Model.IATA);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
            Assert.AreEqual(Manufacturer, aircraft.Model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task LookupViaApiTestAsync()
        {
            _client.AddResponse(Response);

            var aircraft = await _manager.IdentifyAircraftAsync(Address);

            Assert.IsNotNull(aircraft);
            Assert.IsGreaterThan(0, aircraft.Id);
            Assert.AreEqual(Address, aircraft.Address);
            Assert.AreEqual(Registration, aircraft.Registration);
            Assert.IsNull(aircraft.Manufactured);
            Assert.IsNull(aircraft.Age);
            Assert.AreEqual(ModelIATA, aircraft.Model.IATA);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
            Assert.AreEqual(Manufacturer, aircraft.Model.Manufacturer.Name);
        }
    }
}