using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API.Wrapper
{
    [TestClass]
    public class AeroDataBoxFlightLookupManagerTest
    {
        private const string Address = "405A48";
        private const string FlightIATA = "U22123";
        private const string Embarkation = "MAN";
        private const string Destination = "FCO";
        private const string AirlineIATA = "U2";
        private const string AirlineICAO = "EZY";
        private const string AirlineName = "Easyjet";
        private readonly DateTime LastSeen = new(2025, 9, 25, 8, 45, 0);
        private const string Response = "[ { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 07:20Z\", \"local\": \"2025-09-25 08:20+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 07:15Z\", \"local\": \"2025-09-25 08:15+01:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 07:45Z\", \"local\": \"2025-09-25 08:45+01:00\" }, \"terminal\": \"1\", \"gate\": \"4\", \"runway\": \"05L\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 10:10Z\", \"local\": \"2025-09-25 12:10+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 10:04Z\", \"local\": \"2025-09-25 12:04+02:00\" }, \"predictedTime\": { \"utc\": \"2025-09-25 09:56Z\", \"local\": \"2025-09-25 11:56+02:00\" }, \"terminal\": \"1\", \"runway\": \"16R\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 10:11Z\", \"number\": \"U2 2123\", \"callSign\": \"EZY12ND\", \"status\": \"Approaching\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } }, { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 11:00Z\", \"local\": \"2025-09-25 13:00+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"terminal\": \"1\", \"runway\": \"25\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 13:50Z\", \"local\": \"2025-09-25 14:50+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 14:42Z\", \"local\": \"2025-09-25 15:42+01:00\" }, \"terminal\": \"1\", \"gate\": \"9\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 14:47Z\", \"number\": \"U2 2124\", \"callSign\": \"EZY38DT\", \"status\": \"Arrived\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } } ]";

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
        public async Task Initialise()
        {
            // Construct a database management factory
            var logger = new MockFileLogger();
            BaseStationReaderDbContext context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);

            // Add the airline to the database
            _ = await _factory.AirlineManager.AddAsync(AirlineIATA, AirlineICAO, AirlineName);

            // Construct the aircraft lookup management instance
            _client = new MockTrackerHttpClient();
            var api = new ExternalApiFactory().GetApiInstance(ApiServiceType.AeroDataBox, ApiEndpointType.Flights, _client, _factory, _settings);
            var register = new ExternalApiRegister(logger);
            register.RegisterExternalApi(ApiEndpointType.Flights, api);
            var airlineLookupManager = new AirlineLookupManager(register, _factory);
            _manager = new FlightLookupManager(register, _factory, airlineLookupManager);
        }

        [TestMethod]
        public async Task LookupTestAsync()
        {
            _client.AddResponse(Response);
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Status = TrackingStatus.Active,
                LastSeen = LastSeen
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [], []);

            Assert.IsNotNull(flight);
            Assert.IsGreaterThan(0, flight.Id);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.IsEmpty(flight.ICAO);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineName, flight.Airline.Name);
        }

        [TestMethod]
        public async Task LookupWithAcceptingAirportFiltersTestAsync()
        {
            _client.AddResponse(Response);
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Status = TrackingStatus.Active,
                LastSeen = LastSeen
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [Embarkation], [Destination]);

            Assert.IsNotNull(flight);
            Assert.IsGreaterThan(0, flight.Id);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.IsEmpty(flight.ICAO);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineName, flight.Airline.Name);
        }

        [TestMethod]
        public async Task LookupWithRejectingEmbarkationFiltersTestAsync()
        {
            _client.AddResponse("[]");
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Status = TrackingStatus.Active,
                LastSeen = LastSeen
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [Destination], [Destination]);

            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupWithRejectingDestinationFiltersTestAsync()
        {
            _client.AddResponse("[]");
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Status = TrackingStatus.Active,
                LastSeen = LastSeen
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [Embarkation], [Embarkation]);

            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupWithDateTooEarlyTestAsync()
        {
            _client.AddResponse("[]");
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Status = TrackingStatus.Active,
                LastSeen = LastSeen.AddDays(-1)
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [Embarkation], [Embarkation]);

            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupWithDateTooLateTestAsync()
        {
            _client.AddResponse("[]");
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Status = TrackingStatus.Active,
                LastSeen = LastSeen.AddDays(1)
            });

            var flight = await _manager.IdentifyFlightAsync(Address, [Embarkation], [Embarkation]);

            Assert.IsNull(flight);
        }
    }
}