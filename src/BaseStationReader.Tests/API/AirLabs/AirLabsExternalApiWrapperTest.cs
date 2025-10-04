using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API
{
    [TestClass]
    public class AirLabsExternalApiWrapperTest
    {
        private const string AircraftAddress = "4851F6";
        private const int AircraftManufactured = 2018;
        private const string Embarkation = "AMS";
        private const string Destination = "LIM";
        private const string FlightResponse = "{\"response\": [ { \"hex\": \"4851F6\", \"reg_number\": \"PH-BVS\", \"flag\": \"NL\", \"lat\": 51.17756, \"lng\": -2.833342, \"alt\": 9148, \"dir\": 253, \"speed\": 849, \"v_speed\": 0, \"flight_number\": \"743\", \"flight_icao\": \"KLM743\", \"flight_iata\": \"KL743\", \"dep_icao\": \"EHAM\", \"dep_iata\": \"AMS\", \"arr_icao\": \"SPJC\", \"arr_iata\": \"LIM\", \"airline_icao\": \"KLM\", \"airline_iata\": \"KL\", \"aircraft_icao\": \"B77W\", \"updated\": 1758446111, \"status\": \"en-route\", \"type\": \"adsb\" } ]}";
        private const string AirlineResponse = "{\"response\": [ { \"name\": \"KLM Royal Dutch Airlines\", \"iata_code\": \"KL\", \"icao_code\": \"KLM\" } ]}";
        private const string AircraftResponse = "{\"response\": [ { \"hex\": \"4851F6\", \"reg_number\": \"PH-BVS\", \"flag\": \"NL\", \"airline_icao\": \"KLM\", \"airline_iata\": \"KL\", \"seen\": 6777120, \"icao\": \"B77W\", \"iata\": \"77W\", \"model\": \"Boeing 777-300ER pax\", \"engine\": \"jet\", \"engine_count\": \"2\", \"manufacturer\": \"BOEING\", \"type\": \"landplane\", \"category\": \"H\", \"built\": 2018, \"age\": 3, \"msn\": \"61604\", \"line\": null, \"lat\": -20.645375, \"lng\": 17.240996, \"alt\": 9164, \"dir\": 354, \"speed\": 946, \"v_speed\": null, \"squawk\": null, \"last_seen\": \"2025-09-15 23:10:56\" } ]}";
        private const string FlightsInBoundingBoxResponse = "{\"response\": [ { \"hex\": \"4CAA59\", \"reg_number\": \"EI-EVT\", \"flag\": \"IE\", \"lat\": 51.919514, \"lng\": -0.263958, \"alt\": 4002, \"dir\": 235, \"speed\": 670, \"v_speed\": 0, \"flight_number\": \"5552\", \"flight_icao\": \"RYR5552\", \"flight_iata\": \"FR5552\", \"dep_icao\": \"EGSS\", \"dep_iata\": \"STN\", \"arr_icao\": \"LEGE\", \"arr_iata\": \"GRO\", \"airline_icao\": \"RYR\", \"airline_iata\": \"FR\", \"aircraft_icao\": \"B738\", \"updated\": 1758732643, \"status\": \"en-route\", \"type\": \"adsb\" }, { \"hex\": \"4BAA8A\", \"reg_number\": \"TC-JTJ\", \"flag\": \"TR\", \"lat\": 51.168826, \"lng\": 0.022797, \"alt\": 1534, \"dir\": 87, \"speed\": 442, \"v_speed\": 0, \"flight_number\": \"1869\", \"flight_icao\": \"THY1869\", \"flight_iata\": \"TK1869\", \"dep_icao\": \"EGKK\", \"dep_iata\": \"LGW\", \"arr_icao\": \"LTFM\", \"arr_iata\": \"IST\", \"airline_icao\": \"THY\", \"airline_iata\": \"TK\", \"aircraft_icao\": \"A321\", \"updated\": 1758732643, \"status\": \"en-route\", \"type\": \"adsb\" } ]}";

        private MockFileLogger _logger;
        private BaseStationReaderDbContext _context;
        private MockTrackerHttpClient _client;
        private IExternalApiWrapper _wrapper;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.AirLabs, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.Airlines, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.AirLabs, EndpointType = ApiEndpointType.ActiveFlights, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public async Task Initialise()
        {
            _logger = new();
            _client = new();
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var trackedAircraftWriter = new TrackedAircraftWriter(_context);
            _wrapper = ExternalApiFactory.GetWrapperInstance(
                _logger, _client, _context, trackedAircraftWriter, ApiServiceType.AirLabs, ApiEndpointType.ActiveFlights, _settings);

            // Create a tracked aircraft that will match the first flight in the flights response
            _ = await trackedAircraftWriter.WriteAsync(new()
            {
                Address = AircraftAddress
            });
        }

        [TestMethod]
        public async Task LookupAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);
            var result = await _wrapper.LookupAsync(ApiEndpointType.ActiveFlights, AircraftAddress, null, null, true);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task LookupWithAcceptingAirportFiltersAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);
            var result = await _wrapper.LookupAsync(ApiEndpointType.ActiveFlights, AircraftAddress, [Embarkation], [Destination], true);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task LookupWithExcludingAirportFiltersAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);
            var result = await _wrapper.LookupAsync(ApiEndpointType.ActiveFlights, AircraftAddress, [Destination], [Embarkation], true);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public async Task LookupActiveFlightsInBoundingBoxTest()
        {
            _client.AddResponse(FlightsInBoundingBoxResponse);
            var flights = await _wrapper.LookupActiveFlightsInBoundingBox(0, 0, 0);

            Assert.IsNotNull(flights);
            Assert.HasCount(2, flights);

            var flight = flights.Where(x => x.AircraftAddress == "4CAA59").First();
            Assert.IsNotNull(flight);
            Assert.AreEqual("RYR5552", flight.ICAO);
            Assert.AreEqual("FR5552", flight.IATA);
            Assert.AreEqual("FR5552", flight.Number);
            Assert.AreEqual("STN", flight.Embarkation);
            Assert.AreEqual("GRO", flight.Destination);

            flight = flights.Where(x => x.AircraftAddress == "4BAA8A").First();
            Assert.IsNotNull(flight);
            Assert.AreEqual("THY1869", flight.ICAO);
            Assert.AreEqual("TK1869", flight.IATA);
            Assert.AreEqual("TK1869", flight.Number);
            Assert.AreEqual("LGW", flight.Embarkation);
            Assert.AreEqual("IST", flight.Destination);
        }
    }
}