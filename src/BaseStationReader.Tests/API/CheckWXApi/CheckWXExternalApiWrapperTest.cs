using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API
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
                new ApiService() { Service = ApiServiceType.CheckWXApi, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.CheckWXApi, EndpointType = ApiEndpointType.METAR, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var factory = new DatabaseManagementFactory(logger, context, 0, 0);

            _client = new();
            _wrapper = ExternalApiFactory.GetWrapperInstance(
                logger, _client, factory, ApiServiceType.CheckWXApi, ApiEndpointType.ActiveFlights, _settings, false);
        }

        [TestMethod]
        public void GetWeatherTest()
        {
            _client.AddResponse(Response);
            var results = Task.Run(() => _wrapper.LookupCurrentAirportWeatherAsync(AirportICAO)).Result;

            Assert.IsNotNull(results);
            Assert.HasCount(1, results);
            Assert.AreEqual(METAR, results.First());
        }
    }
}