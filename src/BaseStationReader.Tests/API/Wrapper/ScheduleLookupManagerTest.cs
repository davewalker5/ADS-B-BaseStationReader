using System.Text.Json.Nodes;
using BaseStationReader.Api.Wrapper;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Tests.Mocks;
using Moq;

namespace BaseStationReader.Tests.API.Wrapper
{
    [TestClass]
    public class ScheduleLookupManagerTest
    {
        private const string AirportIata = "LHR";
        private readonly DateTime _from = new(2026, 7, 21, 8, 0, 0);
        private readonly DateTime _to = new(2026, 7, 21, 20, 0, 0);
        private ExternalApiRegister _register;
        private IScheduleLookupManager _manager;

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _register = new ExternalApiRegister(logger);
            _manager = new ScheduleLookupManager(logger, _register);
        }

        [TestMethod]
        public async Task LookupSchedulesTestAsync()
        {
            var schedules = JsonNode.Parse("{ \"departures\": [] }");
            var expected = new List<FlightIATACodeMapping> { new() { Callsign = "BAW123" } };
            var api = new Mock<ISchedulesApi>();
            api.Setup(instance => instance.LookupSchedulesRawAsync(AirportIata, _from, _to))
                .ReturnsAsync(schedules);
            api.Setup(instance => instance.ExtractFlightMapping(schedules, AirportIata))
                .Returns(expected);
            _register.RegisterExternalApi(ApiEndpointType.Schedules, api.Object);

            var mappings = await _manager.LookupSchedulesAsync(AirportIata, _from, _to);

            Assert.AreSame(expected, mappings);
            api.Verify(instance => instance.LookupSchedulesRawAsync(AirportIata, _from, _to), Times.Once);
            api.Verify(instance => instance.ExtractFlightMapping(schedules, AirportIata), Times.Once);
        }

        [TestMethod]
        public async Task LookupSchedulesWithNoApiTestAsync()
        {
            var mappings = await _manager.LookupSchedulesAsync(AirportIata, _from, _to);

            Assert.IsNull(mappings);
        }
    }
}
