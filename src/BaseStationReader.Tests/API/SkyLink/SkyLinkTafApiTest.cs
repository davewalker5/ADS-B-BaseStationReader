using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.BusinessLogic.Api.SkyLink;
using System.Threading.Tasks;

namespace BaseStationReader.Tests.API.SkyLink
{
    [TestClass]
    public class SkyLinkTafApiTest
    {
        private const string AirportICAO = "EGLL";
        private const string TAF = "TAF EGLL 021702Z 0218/0324 19012KT 9999 FEW025 PROB30 TEMPO 0220/0303 18015G25KT TEMPO 0223/0305 7000 RA PROB40 TEMPO 0300/0305 3000 +RA BKN012 BECMG 0302/0306 BKN005 TEMPO 0305/0312 6000 -RADZ PROB30 TEMPO 0305/0310 3000 DZ BKN002 BECMG 0312/0315 SCT020 PROB40 TEMPO 0312/0318 20015G25KT 8000 -RA BKN009 BECMG 0318/0320 21018G28KT TEMPO 0318/0324 4000 RADZ BKN009";
        private const string Response = "{ \"raw\": \"TAF EGLL 021702Z 0218/0324 19012KT 9999 FEW025 PROB30 TEMPO 0220/0303 18015G25KT TEMPO 0223/0305 7000 RA PROB40 TEMPO 0300/0305 3000 +RA BKN012 BECMG 0302/0306 BKN005 TEMPO 0305/0312 6000 -RADZ PROB30 TEMPO 0305/0310 3000 DZ BKN002 BECMG 0312/0315 SCT020 PROB40 TEMPO 0312/0318 20015G25KT 8000 -RA BKN009 BECMG 0318/0320 21018G28KT TEMPO 0318/0324 4000 RADZ BKN009\", \"icao\": \"EGLL\", \"airport_name\": \"London Heathrow Airport\", \"timestamp\": \"2025-10-02T19:48:58.316421Z\" }";

        private MockTrackerHttpClient _client = null;
        private ITafApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.SkyLink, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.SkyLink, EndpointType = ApiEndpointType.TAF, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _api = new SkyLinkTafApi(logger, _client, null, _settings);
        }

        [TestMethod]
        public async Task GetWeatherTestAsync()
        {
            _client.AddResponse(Response);
            var results = await _api.LookupAirportWeatherForecastAsync(AirportICAO);

            Assert.IsNotNull(results);
            Assert.HasCount(1, results);
            Assert.AreEqual(TAF, results.First());
        }

        [TestMethod]
        public async Task NullResponseTestAsync()
        {
            _client.AddResponse(null);
            var properties = await _api.LookupAirportWeatherForecastAsync(AirportICAO);

            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task InvalidJsonResponseTestAsync()
        {
            _client.AddResponse("{}");
            var results = await _api.LookupAirportWeatherForecastAsync(AirportICAO);

            Assert.IsNull(results);
        }
    }
}
