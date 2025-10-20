using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.BusinessLogic.Api.SkyLink;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;

namespace BaseStationReader.Tests.API.SkyLink
{
    [TestClass]
    public class SkyLinkMetarApiTest
    {
        private const string AirportICAO = "EGLL";
        private const string METAR = "METAR EGLL 031150Z COR AUTO 19011KT 150V240 9999 BKN005 OVC009 17/16 Q1009 NOSIG";
        private const string Response = "{ \"raw\": \"METAR EGLL 031150Z COR AUTO 19011KT 150V240 9999 BKN005 OVC009 17/16 Q1009 NOSIG\", \"icao\": \"EGLL\", \"airport_name\": \"London Heathrow Airport\", \"timestamp\": \"2025-10-03T12:23:39.122722Z\" }";

        private MockTrackerHttpClient _client = null;
        private IMetarApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.SkyLink, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.METAR, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            var factory = new DatabaseManagementFactory(logger, context, 0, 0);
            _client = new MockTrackerHttpClient();
            _api = new SkyLinkMetarApi(_client, factory, _settings);
        }

        [TestMethod]
        public async Task GetWeatherTestAsync()
        {
            _client.AddResponse(Response);
            var results = await _api.LookupCurrentAirportWeatherAsync(AirportICAO);

            Assert.IsNotNull(results);
            Assert.HasCount(1, results);
            Assert.AreEqual(METAR, results.First());
        }

        [TestMethod]
        public async Task NullResponseTestAsync()
        {
            _client.AddResponse(null);
            var properties = await _api.LookupCurrentAirportWeatherAsync(AirportICAO);

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task InvalidJsonResponseTestAsync()
        {
            _client.AddResponse("{}");
            var results = await _api.LookupCurrentAirportWeatherAsync(AirportICAO);

            Assert.IsNull(results);
        }
    }
}
