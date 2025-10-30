using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API.AirLabs
{
    [TestClass]
    public class AirLabsFlightLookupManagerTest
    {
        private const string Address = "40751C";
        private const string FlightIATA = "LS1347";
        private const string FlightICAO = "EXS1347";
        private const string Embarkation = "BHX";
        private const string Destination = "RHO";
        private const string AirlineIATA = "LS";
        private const string AirlineICAO = "EXS";
        private const string AirlineName = "Jet2";
        private readonly DateTime LastSeen = new(2025, 9, 25, 8, 45, 0);
        private const string Response = "{ \"response\": [ { \"hex\": \"40751C\", \"reg_number\": \"G-DRTD\", \"flag\": \"UK\", \"lat\": 52.005841, \"lng\": -1.361693, \"alt\": 5933, \"dir\": 169, \"speed\": 787, \"v_speed\": 0, \"flight_number\": \"1347\", \"flight_icao\": \"EXS1347\", \"flight_iata\": \"LS1347\", \"dep_icao\": \"EGBB\", \"dep_iata\": \"BHX\", \"arr_icao\": \"LGRP\", \"arr_iata\": \"RHO\", \"airline_icao\": \"EXS\", \"airline_iata\": \"LS\", \"aircraft_icao\": \"B738\", \"updated\": 1761496761, \"status\": \"en-route\", \"type\": \"adsb\" } ]}";

        private IDatabaseManagementFactory _factory;
        private IFlightLookupManager _manager;
        private MockTrackerHttpClient _client;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService()
                {
                    Service = ApiServiceType.AirLabs, Key = "an-api-key",
                    ApiEndpoints = [
                        new ApiEndpoint() { EndpointType = ApiEndpointType.Flights, Url = "http://some.host.com/endpoint"}
                    ]
                }
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

            // Construct the lookup management instance
            _client = new MockTrackerHttpClient();
            var api = new ExternalApiFactory().GetApiInstance(ApiServiceType.AirLabs, ApiEndpointType.Flights, _client, _factory, _settings);
            var register = new ExternalApiRegister(logger);
            register.RegisterExternalApi(ApiEndpointType.Flights, api);
            var airlineLookupManager = new AirlineLookupManager(register, _factory);
            _manager = new FlightLookupManager(register, _factory, airlineLookupManager);
        }

        [TestMethod]
        public async Task LookupTestAsync()
        {
            _client.AddResponse(Response);
            var trackedAircraft = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Status = TrackingStatus.Active,
                LastSeen = LastSeen
            });

            var flight = await _manager.IdentifyFlightAsync(trackedAircraft, [], []);

            Assert.IsNotNull(flight);
            Assert.IsGreaterThan(0, flight.Id);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.AreEqual(FlightICAO, flight.ICAO);
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
            var trackedAircraft = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Status = TrackingStatus.Active,
                LastSeen = LastSeen
            });

            var flight = await _manager.IdentifyFlightAsync(trackedAircraft, [Embarkation], [Destination]);

            Assert.IsNotNull(flight);
            Assert.IsGreaterThan(0, flight.Id);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.AreEqual(FlightICAO, flight.ICAO);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineName, flight.Airline.Name);
        }

        [TestMethod]
        public async Task LookupWithRejectingEmbarkationFiltersTestAsync()
        {
            _client.AddResponse(Response);
            var trackedAircraft = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Status = TrackingStatus.Active,
                LastSeen = LastSeen
            });

            var flight = await _manager.IdentifyFlightAsync(trackedAircraft, [Destination], [Destination]);

            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupWithRejectingDestinationFiltersTestAsync()
        {
            _client.AddResponse(Response);
            var trackedAircraft = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Status = TrackingStatus.Active,
                LastSeen = LastSeen
            });

            var flight = await _manager.IdentifyFlightAsync(trackedAircraft, [Embarkation], [Embarkation]);

            Assert.IsNull(flight);
        }
    }
}