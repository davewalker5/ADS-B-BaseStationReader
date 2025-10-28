using BaseStationReader.Entities.Api;
using BaseStationReader.Api.AirLabs;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.BusinessLogic.Database;
using System.Text.Json.Nodes;
using BaseStationReader.Data;

namespace BaseStationReader.Tests.API.AirLabs
{
    [TestClass]
    public class AirLabsFlightApiTest
    {
        private const string Address = "4005C1";
        private const string Response = "{ \"response\": [ { \"hex\": \"4005C1\", \"flag\": \"UK\", \"lat\": 54.001557, \"lng\": -15.078022, \"alt\": 12516, \"dir\": 93, \"speed\": 900, \"flight_number\": \"172\", \"flight_icao\": \"BAW172\", \"flight_iata\": \"BA172\", \"dep_icao\": \"KJFK\", \"dep_iata\": \"JFK\", \"arr_icao\": \"EGLL\", \"arr_iata\": \"LHR\", \"airline_icao\": \"BAW\", \"airline_iata\": \"BA\", \"aircraft_icao\": \"B772\", \"updated\": 1758434637, \"status\": \"en-route\", \"type\": \"adsb\" } ]}";

        private MockTrackerHttpClient _client = null;
        private IFlightApi _api = null;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService()
                {
                    Service = ApiServiceType.AirLabs, Key = "an-api-key",
                    ApiEndpoints = [
                        new ApiEndpoint() { EndpointType = ApiEndpointType.Flights, Url = "http://some.host.com/endpoint"}
                    ]
                }
            ]
        };

        [TestInitialize]
        public void Initialise()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            var factory = new DatabaseManagementFactory(logger, context, 0, 0);
            _client = new MockTrackerHttpClient();
            _api = new AirLabsFlightApi(_client, factory, _settings);
        }

        [TestMethod]
        public async Task GetActiveFlightTestAsync()
        {
            _client.AddResponse(Response);
            var properties = await _api.LookupFlightsAsync(Address, DateTime.Now);
            AssertPropertiesAreCorrect(properties[0]);
        }

        [TestMethod]
        public async Task NullResponseTestAsync()
        {
            _client.AddResponse(null);
            var properties = await _api.LookupFlightsAsync(Address, DateTime.Now);
            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task InvalidJsonResponseTest()
        {
            _client.AddResponse("{}");
            var properties = await _api.LookupFlightsAsync(Address, DateTime.Now);
            Assert.IsNull(properties);
        }

        [TestMethod]
        public async Task EmptyJsonResponseTestAsync()
        {
            _client.AddResponse("{\"response\": []}");
            var properties = await _api.LookupFlightsAsync(Address, DateTime.Now);
            Assert.IsNull(properties);
        }

        [TestMethod]
        public void ExtractFlightIATATest()
        {
            var json = @"{
                ""flight_iata"": ""BA222"",
                ""flight_number"": """"
            }";

            var node = JsonNode.Parse(json);
            var iata = AirLabsFlightApi.ExtractFlightIATA(node, "BA");

            Assert.AreEqual("BA222", iata);
        }

        [TestMethod]
        public void ExtractFlightIATAFromNumberTest()
        {
            var json = @"{
                ""flight_iata"": """",
                ""flight_number"": ""222""
            }";

            var node = JsonNode.Parse(json);
            var iata = AirLabsFlightApi.ExtractFlightIATA(node, "BA");

            Assert.AreEqual("BA222", iata);
        }

        [TestMethod]
        public void ExtractFlightIATAFromEmptyValuesTest()
        {
            var json = @"{
                ""flight_iata"": """",
                ""flight_number"": """"
            }";

            var node = JsonNode.Parse(json);
            var iata = AirLabsFlightApi.ExtractFlightIATA(node, "");

            Assert.IsEmpty(iata);
        }

        [TestMethod]
        public void ExtractFlightIATAWithNullIATATest()
        {
            var json = @"{
                ""flight_iata"": null,
                ""flight_number"": ""222""
            }";

            var node = JsonNode.Parse(json);
            var iata = AirLabsFlightApi.ExtractFlightIATA(node, "BA");

            Assert.AreEqual("BA222", iata);
        }

        [TestMethod]
        public void ExtractFlightIATAWithMissingIATATest()
        {
            var json = @"{
                ""flight_number"": ""222""
            }";

            var node = JsonNode.Parse(json);
            var iata = AirLabsFlightApi.ExtractFlightIATA(node, "BA");

            Assert.AreEqual("BA222", iata);
        }

        [TestMethod]
        public void ExtractFlightIATAWithNullNumberTest()
        {
            var json = @"{
                ""flight_number"": null
            }";

            var node = JsonNode.Parse(json);
            var iata = AirLabsFlightApi.ExtractFlightIATA(node, "BA");
            Assert.IsEmpty(iata);
        }

        [TestMethod]
        public void ExtractFlightIATAWithMissingNumberTest()
        {
            var json = @"{
            }";

            var node = JsonNode.Parse(json);
            var iata = AirLabsFlightApi.ExtractFlightIATA(node, "BA");
            Assert.IsEmpty(iata);
        }


        [TestMethod]
        public void ExtractFlightIATAFromNullNodeTest()
        {
            var iata = AirLabsFlightApi.ExtractFlightIATA(null, "BA");
            Assert.IsEmpty(iata);
        }

        private static void AssertPropertiesAreCorrect(Dictionary<ApiProperty, string> properties)
        {
            Assert.IsNotNull(properties);
            Assert.HasCount(9, properties);
            Assert.AreEqual("JFK", properties[ApiProperty.EmbarkationIATA]);
            Assert.AreEqual("LHR", properties[ApiProperty.DestinationIATA]);
            Assert.AreEqual("BA172", properties[ApiProperty.FlightIATA]);
            Assert.AreEqual("BAW172", properties[ApiProperty.FlightICAO]);
            Assert.AreEqual("BA", properties[ApiProperty.AirlineIATA]);
            Assert.AreEqual("BAW", properties[ApiProperty.AirlineICAO]);
            Assert.IsEmpty(properties[ApiProperty.AirlineName]);
            Assert.AreEqual("B772", properties[ApiProperty.ModelICAO]);
            Assert.AreEqual("4005C1", properties[ApiProperty.AircraftAddress]);
        }
    }
}
