using BaseStationReader.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API.CheckWXApi
{
    [TestClass]
    public class CheckWXExternalApiWrapperTest
    { 
        private const string AirportICAO = "EGLL";
        private const string METAR = "METAR EGLL 291150Z COR AUTO VRB03KT 9999 SCT028 16/09 Q1026 NOSIG";
        private const string Response = "{ \"results\": 1, \"data\": [ \"METAR EGLL 291150Z COR AUTO VRB03KT 9999 SCT028 16/09 Q1026 NOSIG\" ] }";

        private MockTrackerHttpClient _client;
        private IExternalApiWrapper _wrapper;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService()
                {
                    Service = ApiServiceType.CheckWXApi, Key = "an-api-key",
                    ApiEndpoints = [
                        new ApiEndpoint() { EndpointType = ApiEndpointType.METAR, Url = "http://some.host.com/endpoint"}
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
            _wrapper = new ExternalApiFactory().GetWrapperInstance(_client, factory, ApiServiceType.CheckWXApi, _settings);
        }

        [TestMethod]
        public async Task GetCurrentAirportWeatherTestAsync()
        {
            _client.AddResponse(Response);
            var results = await _wrapper.LookupCurrentAirportWeatherAsync(AirportICAO);

            Assert.IsNotNull(results);
            Assert.HasCount(1, results);
            Assert.AreEqual(METAR, results.First());
        }
    }
}