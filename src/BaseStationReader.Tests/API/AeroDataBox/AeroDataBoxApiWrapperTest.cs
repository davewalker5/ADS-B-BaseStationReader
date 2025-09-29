using System.Globalization;
using BaseStationReader.BusinessLogic.Api.AeroDatabox;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.API.AeroDataBox
{
    /// <summary>
    /// These tests can't test authentication/authorisation at the service end, the lookup of data at the
    /// service end or network transport. They're design to test the downstream logic once a response has
    /// been received
    /// </summary>
    [TestClass]
    public class AirLabsApiWrapperTest
    {
        private const string AircraftAddress = "4074B6";
        private const string AircraftRegistration = "G-UZHF";
        private const int AircraftManufactured = 2018;
        private const string ModelICAO = "A320";
        private const string ModelIATA = "32A";
        private const string ModelName = "Airbus A320 (Sharklets)";
        private const string ManufacturerName = "";
        private const string FlightICAO = "";
        private const string FlightIATA = "";
        private const string FlightNumber = "U22123";
        private const string Embarkation = "MAN";
        private const string Destination = "FCO";
        private const string DepartureTime = "2025-09-25 07:45Z";
        private const string AirlineICAO = "EZY";
        private const string AirlineIATA = "U2";
        private const string AirlineName = "easyJet";
        private const string AircraftResponse = "{ \"id\": 26975, \"reg\": \"G-UZHF\", \"active\": true, \"serial\": \"8193\", \"hexIcao\": \"4074B6\", \"airlineName\": \"easyJet\", \"iataCodeShort\": \"32A\", \"icaoCode\": \"A320\", \"model\": \"A20N\", \"modelCode\": \"320-251N\", \"numSeats\": 186, \"rolloutDate\": \"2018-04-09\", \"firstFlightDate\": \"2018-04-09\", \"deliveryDate\": \"2018-04-17\", \"registrationDate\": \"2018-04-17\", \"typeName\": \"Airbus A320 (Sharklets)\", \"numEngines\": 2, \"engineType\": \"Jet\", \"isFreighter\": false, \"productionLine\": \"Airbus A320\", \"ageYears\": 7.5, \"verified\": true, \"numRegistrations\": 1 }";
        private const string FlightResponse = "[ { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 07:20Z\", \"local\": \"2025-09-25 08:20+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 07:15Z\", \"local\": \"2025-09-25 08:15+01:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 07:45Z\", \"local\": \"2025-09-25 08:45+01:00\" }, \"terminal\": \"1\", \"gate\": \"4\", \"runway\": \"05L\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 10:10Z\", \"local\": \"2025-09-25 12:10+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 10:04Z\", \"local\": \"2025-09-25 12:04+02:00\" }, \"predictedTime\": { \"utc\": \"2025-09-25 09:56Z\", \"local\": \"2025-09-25 11:56+02:00\" }, \"terminal\": \"1\", \"runway\": \"16R\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 10:11Z\", \"number\": \"U2 2123\", \"callSign\": \"EZY12ND\", \"status\": \"Approaching\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } }, { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 11:00Z\", \"local\": \"2025-09-25 13:00+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"terminal\": \"1\", \"runway\": \"25\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 13:50Z\", \"local\": \"2025-09-25 14:50+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 14:42Z\", \"local\": \"2025-09-25 15:42+01:00\" }, \"terminal\": \"1\", \"gate\": \"9\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 14:47Z\", \"number\": \"U2 2124\", \"callSign\": \"EZY38DT\", \"status\": \"Arrived\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } } ]";
        private const string FlightResponseWithMismatchedAddress = "[ { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 07:20Z\", \"local\": \"2025-09-25 08:20+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 07:15Z\", \"local\": \"2025-09-25 08:15+01:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 07:45Z\", \"local\": \"2025-09-25 08:45+01:00\" }, \"terminal\": \"1\", \"gate\": \"4\", \"runway\": \"05L\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 10:10Z\", \"local\": \"2025-09-25 12:10+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 10:04Z\", \"local\": \"2025-09-25 12:04+02:00\" }, \"predictedTime\": { \"utc\": \"2025-09-25 09:56Z\", \"local\": \"2025-09-25 11:56+02:00\" }, \"terminal\": \"1\", \"runway\": \"16R\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 10:11Z\", \"number\": \"U2 2123\", \"callSign\": \"EZY12ND\", \"status\": \"Approaching\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"6B4704\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } }, { \"greatCircleDistance\": { \"meter\": 1679537.5, \"km\": 1679.54, \"mile\": 1043.62, \"nm\": 906.88, \"feet\": 5510293.62 }, \"departure\": { \"airport\": { \"icao\": \"LIRF\", \"iata\": \"FCO\", \"name\": \"Rome Leonardo da Vinci–Fiumicino\", \"shortName\": \"Leonardo da Vinci–Fiumicino\", \"municipalityName\": \"Rome\", \"location\": { \"lat\": 41.8045, \"lon\": 12.2508 }, \"countryCode\": \"IT\", \"timeZone\": \"Europe/Rome\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 11:00Z\", \"local\": \"2025-09-25 13:00+02:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"runwayTime\": { \"utc\": \"2025-09-25 12:16Z\", \"local\": \"2025-09-25 14:16+02:00\" }, \"terminal\": \"1\", \"runway\": \"25\", \"quality\": [ \"Basic\", \"Live\" ] }, \"arrival\": { \"airport\": { \"icao\": \"EGCC\", \"iata\": \"MAN\", \"name\": \"Manchester\", \"shortName\": \"Manchester\", \"municipalityName\": \"Manchester\", \"location\": { \"lat\": 53.3537, \"lon\": -2.27495 }, \"countryCode\": \"GB\", \"timeZone\": \"Europe/London\" }, \"scheduledTime\": { \"utc\": \"2025-09-25 13:50Z\", \"local\": \"2025-09-25 14:50+01:00\" }, \"revisedTime\": { \"utc\": \"2025-09-25 14:42Z\", \"local\": \"2025-09-25 15:42+01:00\" }, \"terminal\": \"1\", \"gate\": \"9\", \"quality\": [ \"Basic\", \"Live\" ] }, \"lastUpdatedUtc\": \"2025-09-25 14:47Z\", \"number\": \"U2 2124\", \"callSign\": \"EZY38DT\", \"status\": \"Arrived\", \"codeshareStatus\": \"IsOperator\", \"isCargo\": false, \"aircraft\": { \"reg\": \"G-UZHF\", \"modeS\": \"4074B6\", \"model\": \"Airbus A320 (Sharklets)\" }, \"airline\": { \"name\": \"easyJet\", \"iata\": \"U2\", \"icao\": \"EZY\" } } ]";

        private ExternalApiSettings _settings;
        private MockFileLogger _logger;
        private BaseStationReaderDbContext _context;
        private MockTrackerHttpClient _client;
        private AeroDataBoxApiWrapper _wrapper;
        private IAirlineManager _airlineManager;
        private IFlightManager _flightManager;
        private IAircraftManager _aircraftManager;
        private IModelManager _modelManager;
        private IManufacturerManager _manufacturerManager;
        private ISightingManager _sightingManager;
        private IAircraftWriter _aircraftWriter;
        private readonly int _aircraftAge = DateTime.Today.Year - AircraftManufactured;

        [TestInitialize]
        public async Task Initialise()
        {
            // Create the settings
            _settings = new()
            {
                ApiServiceKeys = [
                    new ApiService() { Service = ApiServiceType.AeroDataBox, Key = "Some API Key"}
                ],
                ApiEndpoints = [
                    new ApiEndpoint() { Service = ApiServiceType.AeroDataBox, EndpointType = ApiEndpointType.Aircraft, Url = "http://some.host.com/endpoint"},
                    new ApiEndpoint() { Service = ApiServiceType.AeroDataBox, EndpointType = ApiEndpointType.HistoricalFlights, Url = "http://some.host.com/endpoint"}
                ]
            };

            // Create but do not initialise the wrapper
            _context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _wrapper = new AeroDataBoxApiWrapper();

            // Create DB management classes to check entities have been written OK
            _airlineManager = new AirlineManager(_context);
            _flightManager = new FlightManager(_context);
            _aircraftManager = new AircraftManager(_context);
            _modelManager = new ModelManager(_context);
            _manufacturerManager = new ManufacturerManager(_context);
            _sightingManager = new SightingManager(_context);
            _aircraftWriter = new TrackedAircraftWriter(_context);

            // Create a tracked aircraft that will match the first flight in the flights response
            DateTime.TryParse(DepartureTime, null, DateTimeStyles.AdjustToUniversal, out DateTime utc);
            _ = await _aircraftWriter.WriteAsync(new()
            {
                Address = AircraftAddress,
                LastSeen = utc.AddMinutes(30).ToLocalTime()
            });
        }

        [TestMethod]
        public void CannotInitialiseWithMissingKeyTest()
        {
            _settings.ApiServiceKeys[0].Key = "";
            var valid = _wrapper.Initialise(_logger, _client, _context, _settings);
            Assert.IsFalse(valid);
        }

        [TestMethod]
        public void CannotInitialiseWithMissingAircraftEndpointTest()
        {
            _settings.ApiEndpoints.RemoveAll(x => x.EndpointType == ApiEndpointType.Aircraft);
            var valid = _wrapper.Initialise(_logger, _client, _context, _settings);
            Assert.IsFalse(valid);
        }

        [TestMethod]
        public void CannotInitialiseWithEmptyAircraftEndpointTest()
        {
            _settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.Aircraft).Url = "";
            var valid = _wrapper.Initialise(_logger, _client, _context, _settings);
            Assert.IsFalse(valid);
        }

        [TestMethod]
        public void CannotInitialiseWithMissingFlightsEndpointTest()
        {
            _settings.ApiEndpoints.RemoveAll(x => x.EndpointType == ApiEndpointType.HistoricalFlights);
            var valid = _wrapper.Initialise(_logger, _client, _context, _settings);
            Assert.IsFalse(valid);
        }

        [TestMethod]
        public void CannotInitialiseWithEmptyFlightsEndpointTest()
        {
            _settings.ApiEndpoints.First(x => x.EndpointType == ApiEndpointType.HistoricalFlights).Url = "";
            var valid = _wrapper.Initialise(_logger, _client, _context, _settings);
            Assert.IsFalse(valid);
        }

        [TestMethod]
        public async Task LookupAirlineByICAOAsyncTest()
        {
            // This is just a wrapper around stored airline lookup
            _ = await _airlineManager.AddAsync(AirlineIATA, AirlineICAO, AirlineName);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var airline = await _wrapper.LookupAirlineAsync(AirlineICAO, null);

            Assert.IsNotNull(airline);
            Assert.AreEqual(AirlineICAO, airline.ICAO);
            Assert.AreEqual(AirlineIATA, airline.IATA);
            Assert.AreEqual(AirlineName, airline.Name);
        }

        [TestMethod]
        public async Task LookupAirlineByIATAAsyncTest()
        {
            // This is just a wrapper around stored airline lookup
            _ = await _airlineManager.AddAsync(AirlineIATA, AirlineICAO, AirlineName);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var airline = await _wrapper.LookupAirlineAsync(null, AirlineIATA);

            Assert.IsNotNull(airline);
            Assert.AreEqual(AirlineICAO, airline.ICAO);
            Assert.AreEqual(AirlineIATA, airline.IATA);
            Assert.AreEqual(AirlineName, airline.Name);
        }

        [TestMethod]
        public async Task LookupStoredAirlineAsyncTest()
        {
            _ = await _airlineManager.AddAsync(AirlineIATA, AirlineICAO, AirlineName);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var airline = await _wrapper.LookupAirlineAsync(AirlineICAO, null);

            Assert.IsNotNull(airline);
            Assert.AreEqual(AirlineICAO, airline.ICAO);
            Assert.AreEqual(AirlineIATA, airline.IATA);
            Assert.AreEqual(AirlineName, airline.Name);
        }

        [TestMethod]
        public async Task LookupAirlineWithNullCodesAsyncTest()
        {
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var airline = await _wrapper.LookupAirlineAsync(null, null);
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task LookupAirlineWithSimulatedEmptyResponseAsyncTest()
        {
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var airline = await _wrapper.LookupAirlineAsync(AirlineICAO, AirlineIATA);
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task LookupAirlineWithEmptyCodesAsyncTest()
        {
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var airline = await _wrapper.LookupAirlineAsync("", "");
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task LookupAndStoreAirlineAsyncTest()
        {
            // This is just a wrapper around stored airline lookup
            _ = await _airlineManager.AddAsync(AirlineIATA, AirlineICAO, AirlineName);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var airline = await _wrapper.LookupAndStoreAirlineAsync(AirlineICAO, AirlineIATA);
            var retrieved = await _airlineManager.GetAsync(x => x.ICAO == AirlineICAO);

            Assert.IsNotNull(airline);
            Assert.IsGreaterThan(0, airline.Id);
            Assert.AreEqual(AirlineICAO, airline.ICAO);
            Assert.AreEqual(AirlineIATA, airline.IATA);
            Assert.AreEqual(AirlineName, airline.Name);

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(airline.Id, retrieved.Id);
            Assert.AreEqual(airline.ICAO, airline.ICAO);
            Assert.AreEqual(airline.IATA, airline.IATA);
            Assert.AreEqual(airline.Name, airline.Name);
        }

        [TestMethod]
        public async Task LookupAircraftAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var aircraft = await _wrapper.LookupAircraftAsync(AircraftAddress, "");

            Assert.IsNotNull(aircraft);
            Assert.AreEqual(AircraftAddress, aircraft.Address);
            Assert.AreEqual(AircraftRegistration, aircraft.Registration);
            Assert.AreEqual(AircraftManufactured, aircraft.Manufactured);
            Assert.AreEqual(_aircraftAge, aircraft.Age);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
            Assert.AreEqual(ModelIATA, aircraft.Model.IATA);
            Assert.AreEqual(ModelName, aircraft.Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft.Model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task LookupAircraftWithNullAddressAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var aircraft = await _wrapper.LookupAircraftAsync(null, null);
            Assert.IsNull(aircraft);
        }

        [TestMethod]
        public async Task LookupAircraftWithEmptyAddressAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var aircraft = await _wrapper.LookupAircraftAsync("", null);
            Assert.IsNull(aircraft);
        }

        [TestMethod]
        public async Task LookupStoredAircraftAsyncTest()
        {
            var manufacturer = await _manufacturerManager.AddAsync(ManufacturerName);
            var model = await _modelManager.AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);
            _ = await _aircraftManager.AddAsync(AircraftAddress, AircraftRegistration, AircraftManufactured, _aircraftAge, model.Id);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var aircraft = await _wrapper.LookupAircraftAsync(AircraftAddress, null);

            Assert.IsNotNull(aircraft);
            Assert.AreEqual(AircraftAddress, aircraft.Address);
            Assert.AreEqual(AircraftRegistration, aircraft.Registration);
            Assert.AreEqual(AircraftManufactured, aircraft.Manufactured);
            Assert.AreEqual(_aircraftAge, aircraft.Age);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
            Assert.AreEqual(ModelIATA, aircraft.Model.IATA);
            Assert.AreEqual(ModelName, aircraft.Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft.Model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task LookupAndStoreAircraftAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var aircraft = await _wrapper.LookupAndStoreAircraftAsync(AircraftAddress, "");
            var retrieved = await _aircraftManager.GetAsync(x => x.Address == AircraftAddress);
            var model = await _modelManager.GetAsync(x => x.ICAO == ModelICAO);
            var manufacturer = await _manufacturerManager.GetAsync(x => x.Name == ManufacturerName);

            Assert.IsNotNull(aircraft);
            Assert.IsGreaterThan(0, aircraft.Id);
            Assert.AreEqual(AircraftAddress, aircraft.Address);
            Assert.AreEqual(AircraftRegistration, aircraft.Registration);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
            Assert.AreEqual(ModelIATA, aircraft.Model.IATA);
            Assert.AreEqual(ModelName, aircraft.Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft.Model.Manufacturer.Name);

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(aircraft.Id, retrieved.Id);
            Assert.AreEqual(aircraft.Address, retrieved.Address);
            Assert.AreEqual(aircraft.Registration, aircraft.Registration);
            Assert.AreEqual(model.Id, retrieved.ModelId);

            Assert.IsNotNull(model);
            Assert.IsGreaterThan(0, model.Id);
            Assert.AreEqual(ModelICAO, model.ICAO);
            Assert.AreEqual(ModelIATA, model.IATA);
            Assert.AreEqual(ModelName, model.Name);
            Assert.AreEqual(manufacturer.Id, model.ManufacturerId);

            Assert.IsNotNull(manufacturer);
            Assert.IsGreaterThan(0, manufacturer.Id);
            Assert.AreEqual(ManufacturerName, manufacturer.Name);
        }

        [TestMethod]
        public async Task LookupAndStoreAircraftWithSimulatedEmptyResponseAsyncTest()
        {
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var aircraft = await _wrapper.LookupAndStoreAircraftAsync(AircraftAddress, "");
            Assert.IsNull(aircraft);
        }

        [TestMethod]
        public async Task LookupAndStoreAircraftWithStoredModelAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            var manufacturer = await _manufacturerManager.AddAsync(ManufacturerName);
            _ = await _modelManager.AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var aircraft = await _wrapper.LookupAndStoreAircraftAsync(AircraftAddress, "");

            Assert.IsNotNull(aircraft);
            Assert.IsGreaterThan(0, aircraft.Id);
            Assert.AreEqual(AircraftAddress, aircraft.Address);
            Assert.AreEqual(AircraftRegistration, aircraft.Registration);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
            Assert.AreEqual(ModelIATA, aircraft.Model.IATA);
            Assert.AreEqual(ModelName, aircraft.Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft.Model.Manufacturer.Name);
        }

        [TestMethod]
        [ExpectedException(typeof(NotImplementedException))]
        public async Task LookupFlightsInBoundingBoxTest()
        {
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            _ = await _wrapper.LookupFlightsInBoundingBox(51.470020, -0.454295, 10);
        }

        [TestMethod]
        public async Task LookupFlightByNullAddressAsyncTest()
        {
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var flight = await _wrapper.LookupFlightAsync(null, null, null);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupFlightByEmptyAddressAsyncTest()
        {
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var flight = await _wrapper.LookupFlightAsync("", null, null);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupFlightWithNoTrackedAircraftRecordAsyncTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AircraftResponse);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var flight = await _wrapper.LookupFlightAsync("ABC123", null, null);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupFlightWithMismatchedAircraftAddresAsyncTest()
        {
            _client.AddResponse(FlightResponseWithMismatchedAddress);
            _client.AddResponse(AircraftResponse);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var flight = await _wrapper.LookupFlightAsync(AircraftAddress, null, null);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupFlightWithAcceptingAirportFiltersAsyncTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AircraftResponse);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var flight = await _wrapper.LookupFlightAsync(AircraftAddress, [Embarkation], [Destination]);

            Assert.IsNotNull(flight);
            Assert.AreEqual(FlightICAO, flight.ICAO);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.AreEqual(FlightNumber, flight.Number);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);
            Assert.AreEqual(AirlineName, flight.Airline.Name);
        }

        [TestMethod]
        public async Task LookupFlightWithExcludingAirportFiltersAsyncTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AircraftResponse);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var flight = await _wrapper.LookupFlightAsync(AircraftAddress, [Destination], [Embarkation]);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupFlightWithoutAirportFiltersAsyncTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AircraftResponse);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var flight = await _wrapper.LookupFlightAsync(AircraftAddress, null, null);

            Assert.IsNotNull(flight);
            Assert.AreEqual(FlightICAO, flight.ICAO);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.AreEqual(FlightNumber, flight.Number);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);
            Assert.AreEqual(AirlineName, flight.Airline.Name);
        }

        [TestMethod]
        public async Task LookupAndStoreFlightAsyncTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AircraftResponse);
            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var flight = await _wrapper.LookupAndStoreFlightAsync(AircraftAddress, [Embarkation], [Destination]);
            var retrieved = await _flightManager.GetAsync(x => x.ICAO == FlightICAO);
            var airline = await _airlineManager.GetAsync(x => x.ICAO == AirlineICAO);

            Assert.IsNotNull(flight);
            Assert.IsGreaterThan(0, flight.Id);
            Assert.AreEqual(FlightICAO, flight.ICAO);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.AreEqual(FlightNumber, flight.Number);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);

            Assert.IsNotNull(retrieved);
            Assert.AreEqual(flight.Id, retrieved.Id);
            Assert.AreEqual(flight.ICAO, retrieved.ICAO);
            Assert.AreEqual(flight.IATA, retrieved.IATA);
            Assert.AreEqual(flight.Number, retrieved.Number);
            Assert.AreEqual(flight.Embarkation, retrieved.Embarkation);
            Assert.AreEqual(flight.Destination, retrieved.Destination);
            Assert.AreEqual(airline.Id, retrieved.AirlineId);

            Assert.IsGreaterThan(0, airline.Id);
            Assert.AreEqual(AirlineICAO, airline.ICAO);
            Assert.AreEqual(AirlineIATA, airline.IATA);
            Assert.AreEqual(AirlineName, airline.Name);
        }

        [TestMethod]
        public async Task LookupAsyncWithSightingTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AircraftResponse);

            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var result = await _wrapper.LookupAsync(AircraftAddress, [], [], true);
            var flight = await _flightManager.GetAsync(x => x.ICAO == FlightICAO);
            var airline = await _airlineManager.GetAsync(x => x.ICAO == AirlineICAO);
            var aircraft = await _aircraftManager.GetAsync(x => x.Registration == AircraftRegistration);
            var sightings = await _sightingManager.ListAsync(x => true);

            Assert.IsTrue(result.IsSuccessful);

            Assert.IsNotNull(flight);
            Assert.IsGreaterThan(0, flight.Id);
            Assert.AreEqual(FlightICAO, flight.ICAO);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.AreEqual(FlightNumber, flight.Number);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);

            Assert.IsNotNull(airline);
            Assert.IsGreaterThan(0, airline.Id);
            Assert.AreEqual(AirlineICAO, airline.ICAO);
            Assert.AreEqual(AirlineIATA, airline.IATA);
            Assert.AreEqual(AirlineName, airline.Name);

            Assert.IsNotNull(aircraft);
            Assert.IsGreaterThan(0, aircraft.Id);
            Assert.AreEqual(AircraftAddress, aircraft.Address);
            Assert.AreEqual(AircraftRegistration, aircraft.Registration);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
            Assert.AreEqual(ModelIATA, aircraft.Model.IATA);
            Assert.AreEqual(ModelName, aircraft.Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft.Model.Manufacturer.Name);

            Assert.IsNotNull(sightings);
            Assert.HasCount(1, sightings);
            Assert.IsGreaterThan(0, sightings[0].Id);
            Assert.AreEqual(aircraft.Id, sightings[0].AircraftId);
            Assert.AreEqual(flight.Id, sightings[0].FlightId);
        }

        [TestMethod]
        public async Task LookupAsyncWithoutSightingTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AircraftResponse);

            _ = _wrapper.Initialise(_logger, _client, _context, _settings);
            var result = await _wrapper.LookupAsync(AircraftAddress, [], [], false);
            var flight = await _flightManager.GetAsync(x => x.ICAO == FlightICAO);
            var airline = await _airlineManager.GetAsync(x => x.ICAO == AirlineICAO);
            var aircraft = await _aircraftManager.GetAsync(x => x.Registration == AircraftRegistration);
            var sightings = await _sightingManager.ListAsync(x => true);

            Assert.IsTrue(result.IsSuccessful);

            Assert.IsNotNull(flight);
            Assert.IsGreaterThan(0, flight.Id);
            Assert.AreEqual(FlightICAO, flight.ICAO);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.AreEqual(FlightNumber, flight.Number);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);

            Assert.IsNotNull(airline);
            Assert.IsGreaterThan(0, airline.Id);
            Assert.AreEqual(AirlineICAO, airline.ICAO);
            Assert.AreEqual(AirlineIATA, airline.IATA);
            Assert.AreEqual(AirlineName, airline.Name);

            Assert.IsNotNull(aircraft);
            Assert.IsGreaterThan(0, aircraft.Id);
            Assert.AreEqual(AircraftAddress, aircraft.Address);
            Assert.AreEqual(AircraftRegistration, aircraft.Registration);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
            Assert.AreEqual(ModelIATA, aircraft.Model.IATA);
            Assert.AreEqual(ModelName, aircraft.Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft.Model.Manufacturer.Name);

            Assert.IsNotNull(sightings);
            Assert.HasCount(0, sightings);
        }
    }
}