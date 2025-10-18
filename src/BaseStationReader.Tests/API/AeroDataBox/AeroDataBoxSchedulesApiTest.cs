using BaseStationReader.Tests.Mocks;
using BaseStationReader.BusinessLogic.Api.AeroDatabox;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using System.Text.Json.Nodes;
using BaseStationReader.BusinessLogic.Database;

namespace BaseStationReader.Tests.API.AeroDataBox
{
    [TestClass]
    public class AeroDataBoxSchedulesApiTest
    {
        private const string Response = "{ \"departures\": [ { \"movement\": { \"airport\": { \"icao\": \"ESGG\", \"iata\": \"GOT\", \"name\": \"Goteborg\", \"timeZone\": \"Europe/Stockholm\" }, \"scheduledTime\": { \"utc\": \"2025-10-12 07:50Z\", \"local\": \"2025-10-12 09:50\u002B02:00\" }, \"revisedTime\": { \"utc\": \"2025-10-12 07:50Z\", \"local\": \"2025-10-12 09:50\u002B02:00\" }, \"terminal\": \"1\", \"checkInDesk\": \"6-8\", \"gate\": \"D68\", \"quality\": [ \"Basic\", \"Live\" ] }, \"number\": \"KL 1231\", \"callSign\": \"KLM87R\", \"status\": \"Expected\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"PH-BGG\", \"modeS\": \"484966\", \"model\": \"Boeing 737\" }, \"airline\": { \"name\": \"KLM\", \"iata\": \"KL\", \"icao\": \"KLM\" } } ], \"arrivals\": [ { \"movement\": { \"airport\": { \"icao\": \"LEVC\", \"iata\": \"LEVC\", \"name\": \"Valencia\", \"timeZone\": \"Europe/Madrid\" }, \"scheduledTime\": { \"utc\": \"2025-10-12 07:00Z\", \"local\": \"2025-10-12 09:00\u002B02:00\" }, \"revisedTime\": { \"utc\": \"2025-10-12 07:00Z\", \"local\": \"2025-10-12 09:00\u002B02:00\" }, \"runwayTime\": { \"utc\": \"2025-10-12 07:00Z\", \"local\": \"2025-10-12 09:00\u002B02:00\" }, \"terminal\": \"2\", \"baggageBelt\": \"8\", \"quality\": [ \"Basic\", \"Live\" ] }, \"number\": \"KL 1530\", \"callSign\": \"KLM86H\", \"status\": \"Expected\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"PH-NXW\", \"modeS\": \"486804\", \"model\": \"Embraer 195-E2\" }, \"airline\": { \"name\": \"KLM\", \"iata\": \"KL\", \"icao\": \"KLM\" } } ] }";

        private MockTrackerHttpClient _client = null;
        private ISchedulesApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.AeroDataBox, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.AeroDataBox, EndpointType = ApiEndpointType.Schedules, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var logger = new MockFileLogger();
            var factory = new DatabaseManagementFactory(logger, null, 0, 0);
            _client = new MockTrackerHttpClient();
            _api = new AeroDataBoxSchedulesApi(_client, factory, _settings);
        }

        [TestMethod]
        public async Task LookupSchedulesTestAsync()
        {
            _client.AddResponse(Response);
            var schedules = await _api.LookupSchedulesRawAsync("AMS", DateTime.Now, DateTime.Now.AddHours(12));

            (var flight, var airport, var airline) = ExtractFlightDetails(schedules, "departures");
            Assert.AreEqual("ESGG", airport["icao"].ToString());
            Assert.AreEqual("GOT", airport["iata"].ToString());
            Assert.AreEqual("Goteborg", airport["name"].ToString());
            Assert.AreEqual("KL 1231", flight["number"].ToString());
            Assert.AreEqual("KLM87R", flight["callSign"].ToString());
            Assert.AreEqual("KLM", airline["icao"].ToString());
            Assert.AreEqual("KL", airline["iata"].ToString());
            Assert.AreEqual("KLM", airline["name"].ToString());

            (flight, airport, airline) = ExtractFlightDetails(schedules, "arrivals");
            Assert.AreEqual("LEVC", airport["icao"].ToString());
            Assert.AreEqual("LEVC", airport["iata"].ToString());
            Assert.AreEqual("Valencia", airport["name"].ToString());
            Assert.AreEqual("KL 1530", flight["number"].ToString());
            Assert.AreEqual("KLM86H", flight["callSign"].ToString());
            Assert.AreEqual("KLM", airline["icao"].ToString());
            Assert.AreEqual("KL", airline["iata"].ToString());
            Assert.AreEqual("KLM", airline["name"].ToString());
        }

        [TestMethod]
        public async Task LookupSchedulesWithTimespanThatIsNegativeTestAsync()
        {
            _client.AddResponse(Response);
            var schedules = await _api.LookupSchedulesRawAsync("AMS", DateTime.Today.AddDays(1), DateTime.Today);

            Assert.IsNull(schedules);
        }

        [TestMethod]
        public async Task LookupSchedulesWithTimespanThatIsTooLongTestAsync()
        {
            _client.AddResponse(Response);
            var schedules = await _api.LookupSchedulesRawAsync("AMS", DateTime.Today, DateTime.Today.AddDays(1));

            Assert.IsNull(schedules);
        }

        [TestMethod]
        public async Task InvalidJsonResponseTestAsync()
        {
            _client.AddResponse("{}");
            var schedules = await _api.LookupSchedulesRawAsync("AMS", DateTime.Today, DateTime.Today.AddDays(1));

            Assert.IsNull(schedules);
        }

        [TestMethod]
        public async Task NullResponseTestAsync()
        {
            _client.AddResponse(null);
            var properties = await _api.LookupSchedulesRawAsync("AMS", DateTime.Today, DateTime.Today.AddDays(1));

            Assert.IsNull(properties);
        }

        private (JsonObject, JsonObject, JsonObject) ExtractFlightDetails(JsonNode schedules, string flightType)
        {
            var flights = schedules[flightType] as JsonArray;
            var flight = flights[0] as JsonObject;
            var movement = flight["movement"] as JsonObject;
            var airport = movement["airport"] as JsonObject;
            var airline = flight["airline"] as JsonObject;
            return (flight, airport, airline);
        }
    }
}
