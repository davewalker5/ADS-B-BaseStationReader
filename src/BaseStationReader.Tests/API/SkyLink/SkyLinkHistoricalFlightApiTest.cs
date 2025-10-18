using BaseStationReader.Entities.Api;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.BusinessLogic.Api.SkyLink;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Tests.API.SkyLink
{
    [TestClass]
    public class SkyLinkHistoricalFlightApiTest
    {
        private const string Address = "4CAD7A";
        private const string Callsign = "EIN38W";
        private const string Embarkation = "LHR";
        private const string Destination = "SNN";
        private const string FlightIATA = "EI385";
        private const string Response = "{ \"flight_number\": \"EI385\", \"status\": \"Landed 16:13\", \"airline\": \"Aer Lingus\", \"departure\": { \"airport\": \"LHR • London\", \"airport_full\": \"London Heathrow Airport\", \"scheduled_time\": \"14:40\", \"scheduled_date\": \"09 Oct\", \"actual_time\": \"15:19\", \"actual_date\": \"09 Oct\", \"terminal\": \"2\", \"gate\": \"A23\", \"checkin\": \"--\" }, \"arrival\": { \"airport\": \"SNN • Shannon\", \"airport_full\": \"Shannon  International Airport\", \"scheduled_time\": \"16:10\", \"scheduled_date\": \"09 Oct\", \"estimated_time\": \"16:13\", \"estimated_date\": \"09 Oct\", \"terminal\": \"--\", \"gate\": \"--\", \"baggage\": \"--\" } }";

        private MockTrackerHttpClient _client = null;
        private IHistoricalFlightsApi _api = null;
        private IDatabaseManagementFactory _factory;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.SkyLink, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.HistoricalFlights, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);
            _client = new MockTrackerHttpClient();
            _api = new SkyLinkHistoricalFlightApi(_client, _factory, _settings);
        }

        [TestMethod]
        public async Task GetHistoricalFlightsTestAsync()
        {
            // Add a callsign/flight IATA code mapping and a tracked aircraft with that callsign
            await _factory.FlightIATACodeMappingManager.AddAsync("", "", "", "", "", "", AirportType.Unknown, Embarkation, Destination, FlightIATA, Callsign, "");
            await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = Address,
                Callsign = Callsign,
                Status = TrackingStatus.Active
            });

            _client.AddResponse(Response);
            var properties = await _api.LookupFlightsByAircraftAsync(Address, DateTime.Now);

            Assert.IsNotNull(properties);
            Assert.HasCount(1, properties);
            Assert.HasCount(6, properties[0]);
            Assert.IsEmpty(properties[0][ApiProperty.FlightICAO]);
            Assert.AreEqual(FlightIATA, properties[0][ApiProperty.FlightIATA]);
            Assert.AreEqual("EI", properties[0][ApiProperty.AirlineIATA]);
            Assert.IsEmpty(properties[0][ApiProperty.AirlineICAO]);
            Assert.AreEqual("LHR", properties[0][ApiProperty.EmbarkationIATA]);
            Assert.AreEqual("SNN", properties[0][ApiProperty.DestinationIATA]);
        }

        [TestMethod]
        public async Task NullResponseTestAsync()
        {
            _client.AddResponse(null);
            var properties = await _api.LookupFlightsByAircraftAsync(Address, DateTime.Now);

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task InvalidJsonResponseTestAsync()
        {
            _client.AddResponse("{}");
            var properties = await _api.LookupFlightsByAircraftAsync(Address, DateTime.Now);

            Assert.IsNull(properties);
        }
    }
}
