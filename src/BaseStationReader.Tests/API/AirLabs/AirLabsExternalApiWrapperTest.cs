using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API
{
    [TestClass]
    public class AirLabsExternalApiWrapperTest
    {
        private const string AircraftAddress = "4851F6";
        private const string AircraftRegistration = "PH-BVS";
        private const int AircraftManufactured = 2018;
        private const string ModelICAO = "B77W";
        private const string ModelIATA = "77W";
        private const string ModelName = "Boeing 777-300ER pax";
        private const string ManufacturerName = "Boeing";
        private const string Embarkation = "AMS";
        private const string Destination = "LIM";
        private const string AirlineIATA = "KL";
        private const string AirlineICAO = "KLM";
        private const string AirlineName = "Klm Royal Dutch Airlines";
        private const string FlightIATA = "KL743";
        private const string Callsign = "KLM743";
        private const string FlightResponse = "{\"response\": [ { \"hex\": \"4851F6\", \"reg_number\": \"PH-BVS\", \"flag\": \"NL\", \"lat\": 51.17756, \"lng\": -2.833342, \"alt\": 9148, \"dir\": 253, \"speed\": 849, \"v_speed\": 0, \"flight_number\": \"743\", \"flight_icao\": \"KLM743\", \"flight_iata\": \"KL743\", \"dep_icao\": \"EHAM\", \"dep_iata\": \"AMS\", \"arr_icao\": \"SPJC\", \"arr_iata\": \"LIM\", \"airline_icao\": \"KLM\", \"airline_iata\": \"KL\", \"aircraft_icao\": \"B77W\", \"updated\": 1758446111, \"status\": \"en-route\", \"type\": \"adsb\" } ]}";
        private const string AirlineResponse = "{\"response\": [ { \"name\": \"KLM Royal Dutch Airlines\", \"iata_code\": \"KL\", \"icao_code\": \"KLM\" } ]}";
        private const string AircraftResponse = "{\"response\": [ { \"hex\": \"4851F6\", \"reg_number\": \"PH-BVS\", \"flag\": \"NL\", \"airline_icao\": \"KLM\", \"airline_iata\": \"KL\", \"seen\": 6777120, \"icao\": \"B77W\", \"iata\": \"77W\", \"model\": \"Boeing 777-300ER pax\", \"engine\": \"jet\", \"engine_count\": \"2\", \"manufacturer\": \"BOEING\", \"type\": \"landplane\", \"category\": \"H\", \"built\": 2018, \"age\": 3, \"msn\": \"61604\", \"line\": null, \"lat\": -20.645375, \"lng\": 17.240996, \"alt\": 9164, \"dir\": 354, \"speed\": 946, \"v_speed\": null, \"squawk\": null, \"last_seen\": \"2025-09-15 23:10:56\" } ]}";

        private MockTrackerHttpClient _client;
        private IExternalApiWrapper _wrapper;
        private IDatabaseManagementFactory _factory;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.AirLabs, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.Airlines, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.Flights, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public async Task InitialiseAsync()
        {

            // Create a factory that can be used to query the objects that are created during lookup
            var logger = new MockFileLogger();
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);

            _client = new();
            _wrapper = new ExternalApiFactory().GetWrapperInstance(_client, _factory, ApiServiceType.AirLabs, ApiEndpointType.Flights, _settings);

            // Create a tracked aircraft that will match the first flight in the flights response
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = AircraftAddress,
                Callsign = Callsign,
                LastSeen = DateTime.UtcNow
            });


            // Create the model and manufacturer in the database so they'll be picked up during the aircraft
            // lookup
            var manufacturer = await _factory.ManufacturerManager.AddAsync(ManufacturerName);
            await _factory.ModelManager.AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);
        }

        [TestMethod]
        public async Task LookupTestAsync()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);

            var request = new ApiLookupRequest()
            {
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = null,
                ArrivalAirportCodes = null,
                CreateSighting = true
            };

            var result = await _wrapper.LookupAsync(request);

            Assert.IsTrue(result.Successful);
            Assert.IsFalse(result.Requeue);
            await AssertExpectedAircraftCreatedAsync();
            await AssertExpectedAirlineCreatedAsync();
            await AssertExpectedFlightCreatedAsync();
        }

        [TestMethod]
        public async Task LookupWithAcceptingAirportFiltersTestAsync()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);

            var request = new ApiLookupRequest()
            {
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = [Embarkation],
                ArrivalAirportCodes = [Destination],
                CreateSighting = true
            };

            var result = await _wrapper.LookupAsync(request);

            Assert.IsTrue(result.Successful);
            Assert.IsFalse(result.Requeue);
            await AssertExpectedAircraftCreatedAsync();
            await AssertExpectedAirlineCreatedAsync();
            await AssertExpectedFlightCreatedAsync();
        }

        [TestMethod]
        public async Task LookupWithExcludingAirportFiltersTestAsync()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);

            var request = new ApiLookupRequest()
            {
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = [Destination],
                ArrivalAirportCodes = [Embarkation],
                CreateSighting = true
            };

            var result = await _wrapper.LookupAsync(request);
            var flights = await _factory.FlightManager.ListAsync(x => true);
            var airlines = await _factory.AirlineManager.ListAsync(x => true);

            Assert.IsFalse(result.Successful);
            Assert.IsFalse(result.Requeue);
            await AssertExpectedAircraftCreatedAsync();
            Assert.IsEmpty(airlines);
            Assert.IsEmpty(flights);
        }

        private async Task AssertExpectedAircraftCreatedAsync()
        {
            var aircraft = await _factory.AircraftManager.ListAsync(x => true);
            var expectedAge = DateTime.Now.Year - AircraftManufactured;

            Assert.IsNotNull(aircraft);
            Assert.HasCount(1, aircraft);
            Assert.AreEqual(AircraftAddress, aircraft[0].Address);
            Assert.AreEqual(AircraftRegistration, aircraft[0].Registration);
            Assert.AreEqual(AircraftManufactured, aircraft[0].Manufactured);
            Assert.AreEqual(expectedAge, aircraft[0].Age);
            Assert.AreEqual(ModelIATA, aircraft[0].Model.IATA);
            Assert.AreEqual(ModelICAO, aircraft[0].Model.ICAO);
            Assert.AreEqual(ModelName, aircraft[0].Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft[0].Model.Manufacturer.Name);
        }

        private async Task AssertExpectedAirlineCreatedAsync()
        {
            var airlines = await _factory.AirlineManager.ListAsync(x => true);

            Assert.IsNotNull(airlines);
            Assert.HasCount(1, airlines);
            Assert.AreEqual(AirlineIATA, airlines[0].IATA);
            Assert.AreEqual(AirlineICAO, airlines[0].ICAO);
            Assert.AreEqual(AirlineName, airlines[0].Name);
        }

        private async Task AssertExpectedFlightCreatedAsync()
        {
            var flights = await _factory.FlightManager.ListAsync(x => true);

            Assert.IsNotNull(flights);
            Assert.HasCount(1, flights);
            Assert.AreEqual(FlightIATA, flights[0].IATA);
            Assert.AreEqual(AirlineICAO, flights[0].Airline.ICAO);
            Assert.AreEqual(AirlineIATA, flights[0].Airline.IATA);
            Assert.AreEqual(AirlineName, flights[0].Airline.Name);
        }
    }
}