using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API.Wrapper
{
    [TestClass]
    public class FlightLookupManagerTest
    {
        private const string Address = "40751C";
        private const string FlightIATA = "LS1347";
        private const string Callsign = "EXS1347";
        private const string EmbarkationICAO = "EGBB";
        private const string Embarkation = "BHX";
        private const string EmbarkationName = "Birmingham International Airport";
        private const string Destination = "RHO";
        private const string AirlineIATA = "LS";
        private const string AirlineICAO = "EXS";
        private const string AirlineName = "Jet2";

        private IDatabaseManagementFactory _factory;
        private IFlightLookupManager _manager;
        private MockTrackerHttpClient _client;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.AeroDataBox, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.AeroDataBox, EndpointType = ApiEndpointType.Flights, Url = "http://some.host.com/endpoint"}
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
            var api = new ExternalApiFactory().GetApiInstance(ApiServiceType.AeroDataBox, ApiEndpointType.Flights, _client, _factory, _settings);
            var register = new ExternalApiRegister(logger);
            register.RegisterExternalApi(ApiEndpointType.Flights, api);
            _manager = new FlightLookupManager(register, _factory, null);
        }

        [TestMethod]
        public async Task LookupWithNoTrackingRecordTestAsync()
        {
            _client.AddResponse("[]");
            var flight = await _manager.IdentifyFlightAsync(Address, [], []);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupWithInactiveTrackingRecordTestAsync()
        {
            _client.AddResponse("[]");
            await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Callsign = Callsign,
                Status = TrackingStatus.Inactive
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [], []);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupWithNoCallsignTestAsync()
        {
            _client.AddResponse("[]");
            await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Status = TrackingStatus.Active,
                LastSeen = DateTime.UtcNow
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [], []);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupWithNoMappingRecordTestAsync()
        {
            _client.AddResponse("[]");
            await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Callsign = Callsign,
                Status = TrackingStatus.Active,
                LastSeen = DateTime.UtcNow
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [], []);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupFromDatabaseWithExistingFlightTestAsync()
        {
            _ = await _factory.FlightIATACodeMappingManager.AddAsync(AirlineICAO, AirlineIATA, AirlineName, EmbarkationICAO, Embarkation, EmbarkationName, AirportType.Departure, Embarkation, Destination, FlightIATA, Callsign, "Manual");
            var airline = await _factory.AirlineManager.AddAsync(AirlineIATA, AirlineICAO, AirlineName);
            var local = await _factory.FlightManager.AddAsync(FlightIATA, Callsign, Embarkation, Destination, airline.Id);
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Callsign = Callsign,
                Status = TrackingStatus.Active,
                LastSeen = DateTime.UtcNow
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [], []);

            Assert.IsNotNull(flight);
            Assert.AreEqual(local.Id, flight.Id);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.AreEqual(Callsign, flight.ICAO);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineName, flight.Airline.Name);
        }

        [TestMethod]
        public async Task LookupFromDatabaseWithExistingAirlineTestAsync()
        {
            _ = await _factory.FlightIATACodeMappingManager.AddAsync(AirlineICAO, AirlineIATA, AirlineName, EmbarkationICAO, Embarkation, EmbarkationName, AirportType.Departure, Embarkation, Destination, FlightIATA, Callsign, "Manual");
            _ = await _factory.AirlineManager.AddAsync(AirlineIATA, AirlineICAO, AirlineName);
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Callsign = Callsign,
                Status = TrackingStatus.Active,
                LastSeen = DateTime.UtcNow
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [], []);

            Assert.IsNotNull(flight);
            Assert.IsGreaterThan(0, flight.Id);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.IsNull(flight.ICAO);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineName, flight.Airline.Name);
        }

        [TestMethod]
        public async Task LookupFromDatabaseTestAsync()
        {
            _ = await _factory.FlightIATACodeMappingManager.AddAsync(AirlineICAO, AirlineIATA, AirlineName, EmbarkationICAO, Embarkation, EmbarkationName, AirportType.Departure, Embarkation, Destination, FlightIATA, Callsign, "Manual");
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Callsign = Callsign,
                Status = TrackingStatus.Active,
                LastSeen = DateTime.UtcNow
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [], []);

            Assert.IsNotNull(flight);
            Assert.IsGreaterThan(0, flight.Id);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.IsNull(flight.ICAO);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineName, flight.Airline.Name);
        }

        [TestMethod]
        public async Task LookupFromDatabaseWithAcceptingAirportFiltersTestAsync()
        {
            _ = await _factory.FlightIATACodeMappingManager.AddAsync(AirlineICAO, AirlineIATA, AirlineName, EmbarkationICAO, Embarkation, EmbarkationName, AirportType.Departure, Embarkation, Destination, FlightIATA, Callsign, "Manual");
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Callsign = Callsign,
                Status = TrackingStatus.Active,
                LastSeen = DateTime.UtcNow
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [Embarkation], [Destination]);

            Assert.IsNotNull(flight);
            Assert.IsGreaterThan(0, flight.Id);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.IsNull(flight.ICAO);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineName, flight.Airline.Name);
        }

        [TestMethod]
        public async Task LookupFromDatabaseWithRejectingEmbarkationFiltersTestAsync()
        {
            _client.AddResponse("[]");
            _ = await _factory.FlightIATACodeMappingManager.AddAsync(AirlineICAO, AirlineIATA, AirlineName, EmbarkationICAO, Embarkation, EmbarkationName, AirportType.Departure, Embarkation, Destination, FlightIATA, Callsign, "Manual");
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Callsign = Callsign,
                Status = TrackingStatus.Active,
                LastSeen = DateTime.UtcNow
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [Destination], [Destination]);

            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupFromDatabaseWithRejectingDestinationFiltersTestAsync()
        {
            _client.AddResponse("[]");
            _ = await _factory.FlightIATACodeMappingManager.AddAsync(AirlineICAO, AirlineIATA, AirlineName, EmbarkationICAO, Embarkation, EmbarkationName, AirportType.Departure, Embarkation, Destination, FlightIATA, Callsign, "Manual");
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Callsign = Callsign,
                Status = TrackingStatus.Active,
                LastSeen = DateTime.UtcNow
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [Embarkation], [Embarkation]);

            Assert.IsNull(flight);
        }
    }
}