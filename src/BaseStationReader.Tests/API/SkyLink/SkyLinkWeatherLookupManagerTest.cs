using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API.SkyLink
{
    [TestClass]
    public class SkyLinkWeatherLookupManagerTest
    {
        private const string AirportICAO = "EGLL";

        private const string METAR = "METAR EGLL 031150Z COR AUTO 19011KT 150V240 9999 BKN005 OVC009 17/16 Q1009 NOSIG";
        private const string TAF = "TAF EGLL 021702Z 0218/0324 19012KT 9999 FEW025 PROB30 TEMPO 0220/0303 18015G25KT TEMPO 0223/0305 7000 RA PROB40 TEMPO 0300/0305 3000 +RA BKN012 BECMG 0302/0306 BKN005 TEMPO 0305/0312 6000 -RADZ PROB30 TEMPO 0305/0310 3000 DZ BKN002 BECMG 0312/0315 SCT020 PROB40 TEMPO 0312/0318 20015G25KT 8000 -RA BKN009 BECMG 0318/0320 21018G28KT TEMPO 0318/0324 4000 RADZ BKN009";
        private const string MetarResponse = "{ \"raw\": \"METAR EGLL 031150Z COR AUTO 19011KT 150V240 9999 BKN005 OVC009 17/16 Q1009 NOSIG\", \"icao\": \"EGLL\", \"airport_name\": \"London Heathrow Airport\", \"timestamp\": \"2025-10-03T12:23:39.122722Z\" }";
        private const string TafResponse = "{ \"raw\": \"TAF EGLL 021702Z 0218/0324 19012KT 9999 FEW025 PROB30 TEMPO 0220/0303 18015G25KT TEMPO 0223/0305 7000 RA PROB40 TEMPO 0300/0305 3000 +RA BKN012 BECMG 0302/0306 BKN005 TEMPO 0305/0312 6000 -RADZ PROB30 TEMPO 0305/0310 3000 DZ BKN002 BECMG 0312/0315 SCT020 PROB40 TEMPO 0312/0318 20015G25KT 8000 -RA BKN009 BECMG 0318/0320 21018G28KT TEMPO 0318/0324 4000 RADZ BKN009\", \"icao\": \"EGLL\", \"airport_name\": \"London Heathrow Airport\", \"timestamp\": \"2025-10-02T19:48:58.316421Z\" }";

        private MockTrackerHttpClient _client;
        private IWeatherLookupManager _manager;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService()
                {
                    Service = ApiServiceType.SkyLink, Key = "an-api-key",
                    ApiEndpoints = [
                        new ApiEndpoint() { EndpointType = ApiEndpointType.METAR, Url = "http://some.host.com/endpoint"},
                        new ApiEndpoint() { EndpointType = ApiEndpointType.TAF, Url = "http://some.host.com/endpoint"}
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
            var factory = new DatabaseManagementFactory(logger, context, 0, 0);

            // Construct the lookup management instance
            _client = new MockTrackerHttpClient();
            var register = new ExternalApiRegister(logger);
            var metarAPI = new ExternalApiFactory().GetApiInstance(ApiServiceType.SkyLink, ApiEndpointType.METAR, _client, factory, _settings);
            register.RegisterExternalApi(ApiEndpointType.METAR, metarAPI);
            var tafAPI = new ExternalApiFactory().GetApiInstance(ApiServiceType.SkyLink, ApiEndpointType.TAF, _client, factory, _settings);
            register.RegisterExternalApi(ApiEndpointType.TAF, tafAPI);
            _manager = new WeatherLookupManager(logger, register);
        }

        [TestMethod]
        public async Task GetCurrentAirportWeatherTestAsync()
        {
            _client.AddResponse(MetarResponse);
            var results = await _manager.LookupCurrentAirportWeatherAsync(AirportICAO);

            Assert.IsNotNull(results);
            Assert.HasCount(1, results);
            Assert.AreEqual(METAR, results.First());
        }

        [TestMethod]
        public async Task GetAirportWeatherForecastTestAsync()
        {
            _client.AddResponse(TafResponse);
            var results = await _manager.LookupAirportWeatherForecastAsync(AirportICAO);

            Assert.IsNotNull(results);
            Assert.HasCount(1, results);
            Assert.AreEqual(TAF, results.First());
        }
    }
}