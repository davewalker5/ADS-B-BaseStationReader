using BaseStationReader.Entities.Interfaces;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.Tests.Mocks;
using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Database;

namespace BaseStationReader.Tests
{
    /// <summary>
    /// These tests can't test authentication/authorisation at the service end, the lookup of data at the
    /// service end or network transport. They're design to test the downstream logic once a response has
    /// been received
    /// </summary>
    [TestClass]
    public class AirLabsApiWrapperTest
    {
        private const string AirlineICAO = "KLM";
        private const string AirlineIATA = "KL";
        private const string AirlineName = "KLM Royal Dutch Airlines";
        private const string AircraftAddress = "4851F6";
        private const string AircraftRegistration = "PH-BVS";
        private const int AircraftManufactured = 2018;
        private const int AircraftAge = 3;
        private const string ModelICAO = "B77W";
        private const string ModelIATA = "77W";
        private const string ModelName = "Boeing 777-300ER pax";
        private const string ManufacturerName = "BOEING";
        private const string FlightICAO = "KLM743";
        private const string FlightIATA = "KL743";
        private const string FlightNumber = "743";
        private const string Embarkation = "AMS";
        private const string Destination = "LIM";
        private const string FlightResponse = "{\"response\": [ { \"hex\": \"4851F6\", \"reg_number\": \"PH-BVS\", \"flag\": \"NL\", \"lat\": 51.17756, \"lng\": -2.833342, \"alt\": 9148, \"dir\": 253, \"speed\": 849, \"v_speed\": 0, \"flight_number\": \"743\", \"flight_icao\": \"KLM743\", \"flight_iata\": \"KL743\", \"dep_icao\": \"EHAM\", \"dep_iata\": \"AMS\", \"arr_icao\": \"SPJC\", \"arr_iata\": \"LIM\", \"airline_icao\": \"KLM\", \"airline_iata\": \"KL\", \"aircraft_icao\": \"B77W\", \"updated\": 1758446111, \"status\": \"en-route\", \"type\": \"adsb\" } ]}";
        private const string AirlineResponse = "{\"response\": [ { \"name\": \"KLM Royal Dutch Airlines\", \"iata_code\": \"KL\", \"icao_code\": \"KLM\" } ]}";
        private const string AircraftResponse = "{\"response\": [ { \"hex\": \"4851F6\", \"reg_number\": \"PH-BVS\", \"flag\": \"NL\", \"airline_icao\": \"KLM\", \"airline_iata\": \"KL\", \"seen\": 6777120, \"icao\": \"B77W\", \"iata\": \"77W\", \"model\": \"Boeing 777-300ER pax\", \"engine\": \"jet\", \"engine_count\": \"2\", \"manufacturer\": \"BOEING\", \"type\": \"landplane\", \"category\": \"H\", \"built\": 2018, \"age\": 3, \"msn\": \"61604\", \"line\": null, \"lat\": -20.645375, \"lng\": 17.240996, \"alt\": 9164, \"dir\": 354, \"speed\": 946, \"v_speed\": null, \"squawk\": null, \"last_seen\": \"2025-09-15 23:10:56\" } ]}";
        private const string AircraftResponseWithNoModel = "{\"response\": [ { \"hex\": \"4851F6\", \"reg_number\": \"PH-BVS\", \"flag\": \"NL\", \"airline_icao\": \"KLM\", \"airline_iata\": \"KL\", \"seen\": 6777120, \"icao\": null, \"iata\": null, \"model\": null, \"engine\": null, \"engine_count\": null, \"manufacturer\": null, \"type\": null, \"category\": null, \"built\": null, \"age\": null, \"msn\": \"61604\", \"line\": null, \"lat\": -20.645375, \"lng\": 17.240996, \"alt\": 9164, \"dir\": 354, \"speed\": 946, \"v_speed\": null, \"squawk\": null, \"last_seen\": \"2025-09-15 23:10:56\" } ]}";

        private MockTrackerHttpClient _client;
        private AirLabsApiWrapper _wrapper;
        private IAirlineManager _airlineManager;
        private IFlightManager _flightManager;
        private IAircraftManager _aircraftManager;
        private IModelManager _modelManager;
        private IManufacturerManager _manufacturerManager;
        private ISightingManager _sightingManager;

        [TestInitialize]
        public void Initialise()
        {
            // Create the wrapper
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            _wrapper = new AirLabsApiWrapper(logger, _client, context, "", "", "", "");

            // Create DB management classes to check entities have been written OK
            _airlineManager = new AirlineManager(context);
            _flightManager = new FlightManager(context);
            _aircraftManager = new AircraftManager(context);
            _modelManager = new ModelManager(context);
            _manufacturerManager = new ManufacturerManager(context);
            _sightingManager = new SightingManager(context);
        }

        [TestMethod]
        public async Task LookupAirlineByICAOAsyncTest()
        {
            _client.AddResponse(AirlineResponse);
            var airline = await _wrapper.LookupAirlineAsync(AirlineICAO, null);

            Assert.IsNotNull(airline);
            Assert.AreEqual(AirlineICAO, airline.ICAO);
            Assert.AreEqual(AirlineIATA, airline.IATA);
            Assert.AreEqual(AirlineName, airline.Name);
        }

        [TestMethod]
        public async Task LookupAirlineByIATAAsyncTest()
        {
            _client.AddResponse(AirlineResponse);
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
            var airline = await _wrapper.LookupAirlineAsync(AirlineICAO, null);

            Assert.IsNotNull(airline);
            Assert.AreEqual(AirlineICAO, airline.ICAO);
            Assert.AreEqual(AirlineIATA, airline.IATA);
            Assert.AreEqual(AirlineName, airline.Name);
        }

        [TestMethod]
        public async Task LookupAirlineWithNullCodesAsyncTest()
        {
            _client.AddResponse(AirlineResponse);
            var airline = await _wrapper.LookupAirlineAsync(null, null);
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task LookupAirlineWithSimulatedEmptyResponseAsyncTest()
        {
            var airline = await _wrapper.LookupAirlineAsync(AirlineICAO, AirlineIATA);
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task LookupAirlineWithEmptyCodesAsyncTest()
        {
            _client.AddResponse(AirlineResponse);
            var airline = await _wrapper.LookupAirlineAsync("", "");
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task LookupAndStoreAirlineAsyncTest()
        {
            _client.AddResponse(AirlineResponse);
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
        public async Task LookupAndStoreAirlineWithSimulatedEmptyResponseAsyncTest()
        {
            var airline = await _wrapper.LookupAndStoreAirlineAsync(AirlineICAO, AirlineIATA);
            Assert.IsNull(airline);
        }

        [TestMethod]
        public async Task LookupAircraftAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            var aircraft = await _wrapper.LookupAircraftAsync(AircraftAddress, "");

            Assert.IsNotNull(aircraft);
            Assert.AreEqual(AircraftAddress, aircraft.Address);
            Assert.AreEqual(AircraftRegistration, aircraft.Registration);
            Assert.AreEqual(AircraftManufactured, aircraft.Manufactured);
            Assert.AreEqual(AircraftAge, aircraft.Age);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
            Assert.AreEqual(ModelIATA, aircraft.Model.IATA);
            Assert.AreEqual(ModelName, aircraft.Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft.Model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task LookupAircraftWithAlternateModelICAOAsyncTest()
        {
            _client.AddResponse(AircraftResponseWithNoModel);
            var aircraft = await _wrapper.LookupAircraftAsync(AircraftAddress, ModelICAO);

            Assert.IsNotNull(aircraft);
            Assert.AreEqual(AircraftAddress, aircraft.Address);
            Assert.AreEqual(AircraftRegistration, aircraft.Registration);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
        }

        [TestMethod]
        public async Task LookupAircraftWithNullAddressAsyncTest()
        {
            _client.AddResponse(AirlineResponse);
            var aircraft = await _wrapper.LookupAircraftAsync(null, null);
            Assert.IsNull(aircraft);
        }

        [TestMethod]
        public async Task LookupAircraftWithEmptyAddressAsyncTest()
        {
            _client.AddResponse(AirlineResponse);
            var aircraft = await _wrapper.LookupAircraftAsync("", null);
            Assert.IsNull(aircraft);
        }

        [TestMethod]
        public async Task LookupStoredAircraftAsyncTest()
        {
            var manufacturer = await _manufacturerManager.AddAsync(ManufacturerName);
            var model = await _modelManager.AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);
            _ = await _aircraftManager.AddAsync(AircraftAddress, AircraftRegistration, AircraftManufactured, AircraftAge, model.Id);
            var aircraft = await _wrapper.LookupAircraftAsync(AircraftAddress, null);

            Assert.IsNotNull(aircraft);
            Assert.AreEqual(AircraftAddress, aircraft.Address);
            Assert.AreEqual(AircraftRegistration, aircraft.Registration);
            Assert.AreEqual(AircraftManufactured, aircraft.Manufactured);
            Assert.AreEqual(AircraftAge, aircraft.Age);
            Assert.AreEqual(ModelICAO, aircraft.Model.ICAO);
            Assert.AreEqual(ModelIATA, aircraft.Model.IATA);
            Assert.AreEqual(ModelName, aircraft.Model.Name);
            Assert.AreEqual(ManufacturerName, aircraft.Model.Manufacturer.Name);
        }

        [TestMethod]
        public async Task LookupAndStoreAircraftAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
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
            var aircraft = await _wrapper.LookupAndStoreAircraftAsync(AircraftAddress, "");
            Assert.IsNull(aircraft);
        }

        [TestMethod]
        public async Task LookupAndStoreAircraftWithStoredModelAsyncTest()
        {
            _client.AddResponse(AircraftResponse);
            var manufacturer = await _manufacturerManager.AddAsync(ManufacturerName);
            _ = await _modelManager.AddAsync(ModelIATA, ModelICAO, ModelName, manufacturer.Id);
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
        public async Task LookupFlightByNullAddressAsyncTest()
        {
            var flight = await _wrapper.LookupFlightAsync(null, null, null);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupFlightByEmptyAddressAsyncTest()
        {
            var flight = await _wrapper.LookupFlightAsync("", null, null);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupFlightWithAcceptingAirportFiltersAsyncTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);
            _client.AddResponse(AircraftResponse);
            var flight = await _wrapper.LookupFlightAsync(AircraftAddress, [Embarkation], [Destination]);

            Assert.IsNotNull(flight);
            Assert.AreEqual(FlightICAO, flight.ICAO);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.AreEqual(FlightNumber, flight.Number);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);
        }

        [TestMethod]
        public async Task LookupFlightWithExcludingAirportFiltersAsyncTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);
            _client.AddResponse(AircraftResponse);
            var flight = await _wrapper.LookupFlightAsync(AircraftAddress, [Destination], [Embarkation]);
            Assert.IsNull(flight);
        }

        [TestMethod]
        public async Task LookupFlightWithoutAirportFiltersAsyncTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);
            _client.AddResponse(AircraftResponse);
            var flight = await _wrapper.LookupFlightAsync(AircraftAddress, null, null);

            Assert.IsNotNull(flight);
            Assert.AreEqual(FlightICAO, flight.ICAO);
            Assert.AreEqual(FlightIATA, flight.IATA);
            Assert.AreEqual(FlightNumber, flight.Number);
            Assert.AreEqual(Embarkation, flight.Embarkation);
            Assert.AreEqual(Destination, flight.Destination);
            Assert.AreEqual(AirlineICAO, flight.Airline.ICAO);
            Assert.AreEqual(AirlineIATA, flight.Airline.IATA);
        }

        [TestMethod]
        public async Task LookupAndStoreFlightAsyncTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);
            _client.AddResponse(AircraftResponse);
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
        public async Task LookupAsyncTest()
        {
            _client.AddResponse(FlightResponse);
            _client.AddResponse(AirlineResponse);
            _client.AddResponse(AircraftResponse);

            await _wrapper.LookupAsync(AircraftAddress, [], [], true);
            var flight = await _flightManager.GetAsync(x => x.ICAO == FlightICAO);
            var airline = await _airlineManager.GetAsync(x => x.ICAO == AirlineICAO);
            var aircraft = await _aircraftManager.GetAsync(x => x.Registration == AircraftRegistration);
            var sightings = await _sightingManager.ListAsync(x => true);

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
    }
}