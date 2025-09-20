using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests
{
    [TestClass]
    public class AirLabsActiveFlightApiTest
    {
        private const string AircraftAddress = "4CAC23";
        private const string Response = "{\"response\": [{\"hex\": \"4CAC23\",\"reg_number\": \"EI-HGL\",\"flag\": \"IE\",\"lat\": 40.733487,\"lng\": -0.049688,\"alt\": 10683,\"dir\": 192.1,\"speed\": 822,\"v_speed\": -5.5,\"squawk\": \"2074\",\"flight_number\": \"4N\",\"flight_icao\": \"RYR4N\",\"flight_iata\": \"FR9073\",\"dep_icao\": \"EGCC\",\"dep_iata\": \"MAN\",\"arr_icao\": \"LEAL\",\"arr_iata\": \"ALC\",\"airline_icao\": \"RYR\",\"airline_iata\": \"FR\",\"aircraft_icao\": \"B38M\",\"updated\": 1695907120,\"status\": \"en-route\"}]}";

        private MockTrackerHttpClient _client = null;
        private IActiveFlightApi _api = null;

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _api = new AirLabsActiveFlightApi(logger, _client, "", "");
        }

        [TestMethod]
        public void GetActiveFlightTest()
        {
            _client!.AddResponse(Response);
            var properties = Task.Run(() => _api!.LookupFlightByAircraftAsync(AircraftAddress)).Result;

            Assert.IsNotNull(properties);
            Assert.AreEqual(4, properties.Count);
            Assert.AreEqual("MAN", properties[ApiProperty.EmbarkationIATA]);
            Assert.AreEqual("ALC", properties[ApiProperty.DestinationIATA]);
            Assert.AreEqual("FR9073", properties[ApiProperty.FlightIATA]);
            Assert.AreEqual("RYR4N", properties[ApiProperty.FlightICAO]);
        }

        [TestMethod]
        public void InvalidJsonResponseTest()
        {
            _client!.AddResponse("{}");
            var properties = Task.Run(() => _api!.LookupFlightByAircraftAsync(AircraftAddress)).Result;

            Assert.IsNull(properties);
        }

        [TestMethod]
        public void ClientExceptionTest()
        {
            _client!.AddResponse(null);
            var properties = Task.Run(() => _api!.LookupFlightByAircraftAsync(AircraftAddress)).Result;

            Assert.IsNull(properties);
        }
    }
}
