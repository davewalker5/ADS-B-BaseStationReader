using System.Globalization;
using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API
{
    [TestClass]
    public class AeroDataBoxExternalApiWrapperTest
    {
        private const string AircraftAddress = "4074B6";
        private const string AircraftRegistration = "G-UZHF";
        private const int AircraftManufactured = 2018;
        private const string ModelICAO = "A320";
        private const string ModelIATA = "32A";
        private const string ModelName = "320-251N";
        private const string ManufacturerName = "Airbus";
        private const string Embarkation = "MAN";
        private const string Destination = "FCO";
        private const string DepartureTime = "2025-09-25 07:45Z";
        private const string AirlineIATA = "U2";
        private const string AirlineICAO = "EZY";
        private const string AirlineName = "easyJet";
        private const string FlightNumber = "U22123";
        private const string Callsign = "EZY12ND";
        private const string AircraftResponse = "{ \"id\": 26975, \"reg\": \"G-UZHF\", \"active\": true, \"serial\": \"8193\", \"hexIcao\": \"4074B6\", \"airlineName\": \"easyJet\", \"iataCodeShort\": \"32A\", \"icaoCode\": \"A320\", \"model\": \"A20N\", \"modelCode\": \"320-251N\", \"numSeats\": 186, \"rolloutDate\": \"2018-04-09\", \"firstFlightDate\": \"2018-04-09\", \"deliveryDate\": \"2018-04-17\", \"registrationDate\": \"2018-04-17\", \"typeName\": \"Airbus A320 (Sharklets)\", \"numEngines\": 2, \"engineType\": \"Jet\", \"isFreighter\": false, \"productionLine\": \"Airbus A320\", \"ageYears\": 7.5, \"verified\": true, \"numRegistrations\": 1 }";
        private const string FlightResponse = "[ { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 07:20Z\", \"local\": \"2025-09-25 08:20+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 07:15Z\", \"local\": \"2025-09-25 08:15+01:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 07:45Z\", \"local\": \"2025-09-25 08:45+01:00\" }, \"terminal\": \"1\", \"gate\": \"4\", \"runway\": \"05L\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 10:10Z\", \"local\": \"2025-09-25 12:10+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 10:04Z\", \"local\": \"2025-09-25 12:04+02:00\" }, \"predictedTime\": { \"utc\": \"2025-09-25 09:56Z\", \"local\": \"2025-09-25 11:56+02:00\" }, \"terminal\": \"1\", \"runway\": \"16R\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 10:11Z\", \"number\": \"U2 2123\", \"callSign\": \"EZY12ND\", \"status\": \"Approaching\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } }, { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 11:00Z\", \"local\": \"2025-09-25 13:00+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"terminal\": \"1\", \"runway\": \"25\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 13:50Z\", \"local\": \"2025-09-25 14:50+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 14:42Z\", \"local\": \"2025-09-25 15:42+01:00\" }, \"terminal\": \"1\", \"gate\": \"9\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 14:47Z\", \"number\": \"U2 2124\", \"callSign\": \"EZY38DT\", \"status\": \"Arrived\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } } ]";

        private MockTrackerHttpClient _client;
        private IExternalApiWrapper _wrapper;
        private IDatabaseManagementFactory _factory;

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

            // Create a factory that can be used to query the objects that are created during lookup
            var logger = new MockFileLogger();
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(logger, context, 0, 0);

            _client = new();
            _wrapper = ExternalApiFactory.GetWrapperInstance(
                logger, _client, _factory, ApiServiceType.AeroDataBox, ApiEndpointType.HistoricalFlights, _settings, false);

            // Create a tracked aircraft that will match the first flight in the flights response
            DateTime.TryParse(DepartureTime, null, DateTimeStyles.AdjustToUniversal, out DateTime utc);
            _ = await _factory.TrackedAircraftWriter.WriteAsync(new()
            {
                Address = AircraftAddress,
                Callsign = Callsign,
                LastSeen = utc.AddMinutes(30).ToLocalTime(),
                Status = TrackingStatus.Active
            });

            // Create the model and manufacturer in the database so they'll be picked up during the aircraft
            // lookup
            var manufacturer = await _factory.ManufacturerManager.AddAsync(ManufacturerName);
            await _factory.ModelManager.AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);
        }

        [TestMethod]
        public async Task LookupAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);

            var request = new ApiLookupRequest()
            {
                FlightEndpointType = ApiEndpointType.HistoricalFlights,
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = null,
                ArrivalAirportCodes = null,
                CreateSighting = true
            };

            var result = await _wrapper.LookupAsync(request);

            Assert.IsTrue(result.Successful);
            Assert.IsFalse(result.Requeue);
            await AssertExpectedAircraftCreated();
            await AssertExpectedAirlineCreated();
            await AssertExpectedFlightCreated();
        }

        [TestMethod]
        public async Task LookupWithAcceptingAirportFiltersAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);

            var request = new ApiLookupRequest()
            {
                FlightEndpointType = ApiEndpointType.HistoricalFlights,
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = [Embarkation],
                ArrivalAirportCodes = [Destination],
                CreateSighting = true
            };

            var result = await _wrapper.LookupAsync(request);

            Assert.IsTrue(result.Successful);
            Assert.IsFalse(result.Requeue);
            await AssertExpectedAircraftCreated();
            await AssertExpectedAirlineCreated();
            await AssertExpectedFlightCreated();
        }

        [TestMethod]
        public async Task LookupWithExcludingAirportFiltersAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _client.AddResponse(FlightResponse);

            var request = new ApiLookupRequest()
            {
                FlightEndpointType = ApiEndpointType.HistoricalFlights,
                AircraftAddress = AircraftAddress,
                DepartureAirportCodes = [Destination],
                ArrivalAirportCodes = [Embarkation],
                CreateSighting = true
            };

            var result = await _wrapper.LookupAsync(request);
            var flights = await _factory.FlightManager.ListAsync(x => true);
            var airlines = await _factory.AirlineManager.ListAsync(x => true);

            Assert.IsFalse(result.Successful);
            Assert.IsFalse(result.Requeue);
            await AssertExpectedAircraftCreated();
            Assert.IsEmpty(airlines);
            Assert.IsEmpty(flights);
        }

        private async Task AssertExpectedAircraftCreated()
        {
            var aircraft = await _factory.AircraftManager.ListAsync(x => true);
            var expectedAge = DateTime.Now.Year - 2018;

            Assert.IsNotNull(aircraft);
            Assert.HasCount(1, aircraft);
            Assert.AreEqual(AircraftAddress, aircraft[0].Address);
            Assert.AreEqual(AircraftRegistration, aircraft[0].Registration);
            Assert.AreEqual(AircraftManufactured, aircraft[0].Manufactured);
            Assert.AreEqual(expectedAge, aircraft[0].Age);
            Assert.AreEqual(ModelIATA, aircraft[0].Model.IATA);
            Assert.AreEqual(ModelICAO, aircraft[0].Model.ICAO);
            Assert.AreEqual(ModelName, aircraft[0].Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft[0].Model.Manufacturer.Name);
        }

        private async Task AssertExpectedAirlineCreated()
        {
            var airlines = await _factory.AirlineManager.ListAsync(x => true);

            Assert.IsNotNull(airlines);
            Assert.HasCount(1, airlines);
            Assert.AreEqual(AirlineIATA, airlines[0].IATA);
            Assert.AreEqual(AirlineICAO, airlines[0].ICAO);
            Assert.AreEqual(AirlineName, airlines[0].Name);
        }

        private async Task AssertExpectedFlightCreated()
        {
            var flights = await _factory.FlightManager.ListAsync(x => true);

            Assert.IsNotNull(flights);
            Assert.HasCount(1, flights);
            Assert.AreEqual(FlightNumber, flights[0].Number);
            Assert.AreEqual(AirlineICAO, flights[0].Airline.ICAO);
            Assert.AreEqual(AirlineIATA, flights[0].Airline.IATA);
            Assert.AreEqual(AirlineName, flights[0].Airline.Name);
        }
    }
}