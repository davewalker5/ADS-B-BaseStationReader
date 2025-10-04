using System.Globalization;
using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API
{
    [TestClass]
    public class AeroDataBoxExternalApiWrapperTest
    {
        private const string AircraftAddress = "4074B6";
        private const string Embarkation = "MAN";
        private const string Destination = "FCO";
        private const string DepartureTime = "2025-09-25 07:45Z";
        private const string AircraftResponse = "{ \"id\": 26975, \"reg\": \"G-UZHF\", \"active\": true, \"serial\": \"8193\", \"hexIcao\": \"4074B6\", \"airlineName\": \"easyJet\", \"iataCodeShort\": \"32A\", \"icaoCode\": \"A320\", \"model\": \"A20N\", \"modelCode\": \"320-251N\", \"numSeats\": 186, \"rolloutDate\": \"2018-04-09\", \"firstFlightDate\": \"2018-04-09\", \"deliveryDate\": \"2018-04-17\", \"registrationDate\": \"2018-04-17\", \"typeName\": \"Airbus A320 (Sharklets)\", \"numEngines\": 2, \"engineType\": \"Jet\", \"isFreighter\": false, \"productionLine\": \"Airbus A320\", \"ageYears\": 7.5, \"verified\": true, \"numRegistrations\": 1 }";
        private const string FlightResponse = "[ { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 07:20Z\", \"local\": \"2025-09-25 08:20+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 07:15Z\", \"local\": \"2025-09-25 08:15+01:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 07:45Z\", \"local\": \"2025-09-25 08:45+01:00\" }, \"terminal\": \"1\", \"gate\": \"4\", \"runway\": \"05L\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 10:10Z\", \"local\": \"2025-09-25 12:10+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 10:04Z\", \"local\": \"2025-09-25 12:04+02:00\" }, \"predictedTime\": { \"utc\": \"2025-09-25 09:56Z\", \"local\": \"2025-09-25 11:56+02:00\" }, \"terminal\": \"1\", \"runway\": \"16R\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 10:11Z\", \"number\": \"U2 2123\", \"callSign\": \"EZY12ND\", \"status\": \"Approaching\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } }, { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 11:00Z\", \"local\": \"2025-09-25 13:00+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"terminal\": \"1\", \"runway\": \"25\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 13:50Z\", \"local\": \"2025-09-25 14:50+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 14:42Z\", \"local\": \"2025-09-25 15:42+01:00\" }, \"terminal\": \"1\", \"gate\": \"9\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 14:47Z\", \"number\": \"U2 2124\", \"callSign\": \"EZY38DT\", \"status\": \"Arrived\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } } ]";

        private MockFileLogger _logger;
        private BaseStationReaderDbContext _context;
        private MockTrackerHttpClient _client;
        private IExternalApiWrapper _wrapper;

        private readonly ExternalApiSettings _settings = new()
        {
            ApiServices = [
                new ApiService() { Service = ApiServiceType.AeroDataBox, Key = "an-api-key"}
            ],
            ApiEndpoints = [
                new ApiEndpoint() { Service = ApiServiceType.AeroDataBox, EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"},
                new ApiEndpoint() { Service = ApiServiceType.AeroDataBox, EndpointType = ApiEndpointType.HistoricalFlights, Url = "http://some.host.com/endpoint"}
            ]
        };

        [TestInitialize]
        public async Task Initialise()
        {
            _logger = new();
            _client = new();
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var trackedAircraftWriter = new TrackedAircraftWriter(_context);
            _wrapper = ExternalApiFactory.GetWrapperInstance(
                _logger, _client, _context, trackedAircraftWriter, ApiServiceType.AeroDataBox, ApiEndpointType.HistoricalFlights, _settings);

            // Create a tracked aircraft that will match the first flight in the flights response
            DateTime.TryParse(DepartureTime, null, DateTimeStyles.AdjustToUniversal, out DateTime utc);
            _ = await trackedAircraftWriter.WriteAsync(new()
            {
                Address = AircraftAddress,
                LastSeen = utc.AddMinutes(30).ToLocalTime()
            });
        }

        [TestMethod]
        public async Task LookupAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            var result = await _wrapper.LookupAsync(ApiEndpointType.HistoricalFlights, AircraftAddress, null, null, true);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task LookupWithAcceptingAirportFiltersAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            var result = await _wrapper.LookupAsync(ApiEndpointType.HistoricalFlights, AircraftAddress, [Embarkation], [Destination], true);

            Assert.IsTrue(result);
        }

        [TestMethod]
        public async Task LookupWithExcludingAirportFiltersAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);
            var result = await _wrapper.LookupAsync(ApiEndpointType.HistoricalFlights, AircraftAddress, [Destination], [Embarkation], true);

            Assert.IsFalse(result);
        }
    }
}