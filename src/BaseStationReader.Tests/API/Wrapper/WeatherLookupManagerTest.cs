using BaseStationReader.Api.Wrapper;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API.Wrapper
{
    [TestClass]
    public class WeatherLookupManagerTest
    {
        private const string AirportICAO = "EGLL";

        private IWeatherLookupManager _manager;

        [TestInitialize]
        public void Initialise()
        {
            // Construct the lookup management instance - do not register an API on purpose, as these
            // tests check the code that checks the type of the API instance
            var logger = new MockFileLogger();
            var register = new ExternalApiRegister(logger);
            _manager = new WeatherLookupManager(logger, register);
        }

        [TestMethod]
        public async Task GetCurrentAirportWeatherWithNoApiTestAsync()
        {
            var results = await _manager.LookupCurrentAirportWeatherAsync(AirportICAO);
            Assert.IsNull(results);
        }

        [TestMethod]
        public async Task GetAirportWeatherForecastWithNoApiTestAsync()
        {
            var results = await _manager.LookupAirportWeatherForecastAsync(AirportICAO);
            Assert.IsNull(results);
        }
    }
}