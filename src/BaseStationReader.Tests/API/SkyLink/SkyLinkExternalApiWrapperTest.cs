using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API
{
    // [TestClass]
    public class ASkyLinkExternalApiWrapperTest
    {
        private const string AircraftAddress = "4851F6";
        private const int AircraftManufactured = 2018;
        private const string Embarkation = "AMS";
        private const string Destination = "LIM";
        private const string FlightResponse = "{ \"flight_number\": \"BA2277\", \"status\": \"Estimated 17:04\", \"airline\": \"British Airways\", \"departure\": { \"airport\": \"LGW • London\", \"airport_full\": \"London Gatwick Airport\", \"scheduled_time\": \"13:00\", \"scheduled_date\": \"04 Oct\", \"actual_time\": \"17:04\", \"actual_date\": \"04 Oct\", \"terminal\": \"S\", \"gate\": \"25\", \"checkin\": \"--\" }, \"arrival\": { \"airport\": \"LAS • Las Vegas\", \"airport_full\": \"Las Vegas Harry Reid International Airport\", \"scheduled_time\": \"16:00\", \"scheduled_date\": \"04 Oct\", \"estimated_time\": \"--:--\", \"estimated_date\": \"\", \"terminal\": \"3\", \"gate\": \"--\", \"baggage\": \"--\" } }";
        private const string AirlineResponse = "{\"response\": [ { \"name\": \"KLM Royal Dutch Airlines\", \"iata_code\": \"KL\", \"icao_code\": \"KLM\" } ]}";
        private const string AircraftResponse = "{ \"aircraft\": [ { \"icao24\": \"4007EE\", \"callsign\": \"BAW2277\", \"latitude\": 51.569641, \"longitude\": 0.058866, \"altitude\": 21625.0, \"ground_speed\": 396.913086, \"track\": 299.92392, \"vertical_rate\": 1472.0, \"is_on_ground\": false, \"last_seen\": \"2025-10-04T12:33:15.270322\", \"first_seen\": \"2025-09-28T13:43:11.068109\", \"registration\": \"G-YMMC\", \"aircraft_type\": \"B772\", \"airline\": \"British Airways\" } ], \"total_count\": 1, \"timestamp\": \"2025-10-04T12:33:18.014179\" }";

        private MockFileLogger _logger;
        private BaseStationReaderDbContext _context;
        private MockTrackerHttpClient _client;
        private IExternalApiWrapper _wrapper;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.SkyLink, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.Airlines, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.ActiveFlights, Url = "http://some.host.com/endpoint"}
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
                _logger, _client, _context, trackedAircraftWriter, ApiServiceType.SkyLink, ApiEndpointType.ActiveFlights, _settings);

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
    }
}