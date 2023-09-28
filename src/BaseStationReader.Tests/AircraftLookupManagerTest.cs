using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Logic.Api.AirLabs;
using BaseStationReader.Logic.Database;
using BaseStationReader.Logic.Tracking;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests
{
    /// <summary>
    /// These tests can't test authentication/authorisation at the service end, the lookup of data at the
    /// service end or network transport. They're design to test the downstream logic once a response has
    /// been received
    /// </summary>
    [TestClass]
    public class AircraftLookupManagerTest
    {
        private const string AircraftAddress = "3C5468";
        private const string ManufacturerName = "Embraer";
        private const string NotFoundResponse = "[]";
        private const string NoModelDetailsResponse = "{\"response\": [{\"hex\": \"3C5468\",\"airline_icao\": \"DLH\",\"airline_iata\": \"LH\",\"manufacturer\": null,\"icao\": null,\"iata\": null}]}";
        private const string NoAirlineDetailsResponse = "{\"response\": [{\"hex\": \"3C5468\",\"airline_icao\": null,\"airline_iata\": null,\"manufacturer\": \"EMBRAER\",\"icao\": \"E190\",\"iata\": \"E90\"}]}";
        private const string AirlineWithNoIATAResponse = "{\"response\": [{\"hex\": \"3C5468\",\"airline_icao\": \"DLH\",\"airline_iata\": null,\"manufacturer\": \"EMBRAER\",\"icao\": \"E190\",\"iata\": \"E90\"}]}";
        private const string FullResponse = "{\"response\": [{\"hex\": \"3C5468\",\"airline_icao\": \"DLH\",\"airline_iata\": \"LH\",\"manufacturer\": \"EMBRAER\",\"icao\": \"E190\",\"iata\": \"E90\"}]}";
        private const string AirlineResponse = "{\"response\": [{\"name\": \"Lufthansa\",\"iata_code\": \"LH\",\"icao_code\": \"DLH\"}]}";

        private MockTrackerHttpClient? _client = null;
        private IAircraftLookupManager? _manager = null;
        private IAirlineManager? _airlines = null;
        private IAircraftDetailsManager? _details = null;
        private IModelManager? _models = null;
        private int _manufacturerId;

        [TestInitialize]
        public void Initialise()
        {
            // Create a database context
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();

            // Create the database management instances
            var manufacturerManager = new ManufacturerManager(context);
            _airlines = new AirlineManager(context);
            _details = new AircraftDetailsManager(context);
            _models = new ModelManager(context);

            // Add a manufacturer to the database
            _manufacturerId = Task.Run(() => manufacturerManager.AddAsync(ManufacturerName)).Result.Id;

            // Create the API wrappers
            var logger = new MockFileLogger();
            _client = new MockTrackerHttpClient();
            var airlinesApi = new AirLabsAirlinesApi(logger, _client, "", "");
            var aircraftApi = new AirLabsAircraftApi(logger, _client, "", "");

            // Finally, create a lookup manager
            _manager = new AircraftLookupManager(_airlines, _details, _models, airlinesApi, aircraftApi);
        }

        [TestMethod]
        public void AircraftNotFoundTest()
        {
            _client!.AddResponse(NotFoundResponse);
            var details = Task.Run(() => _manager!.LookupAircraft(AircraftAddress)).Result;
            Assert.IsNull(details);
        }

        [TestMethod]
        public void NoModelDetailsTest()
        {
            _client!.AddResponse(NoModelDetailsResponse);
            var details = Task.Run(() => _manager!.LookupAircraft(AircraftAddress)).Result;
            Assert.IsNull(details);
        }

        [TestMethod]
        public void ModelNotInDatabaseTest()
        {
            _client!.AddResponse(FullResponse);
            var details = Task.Run(() => _manager!.LookupAircraft(AircraftAddress)).Result;
            Assert.IsNull(details);
        }

        [TestMethod]
        public void NoAirlineDetailsTest()
        {
            Task.Run(() => _models!.AddAsync("E90", "E190", "190 / Lineage 1000", _manufacturerId)).Wait();
            _client!.AddResponse(NoAirlineDetailsResponse);
            var details = Task.Run(() => _manager!.LookupAircraft(AircraftAddress)).Result;

            Assert.IsNotNull(details);
            Assert.IsNull(details.Airline);
            Assert.IsNotNull(details.Model);
            Assert.AreEqual(AircraftAddress, details.Address);
            Assert.AreEqual("E90", details.Model.IATA);
            Assert.AreEqual("E190", details.Model.ICAO);
            Assert.AreEqual("190 / Lineage 1000", details.Model.Name);
            Assert.AreEqual("Embraer", details.Model.Manufacturer.Name);
        }

        [TestMethod]
        public void AirlineNotInDatabaseTest()
        {
            Task.Run(() => _models!.AddAsync("E90", "E190", "190 / Lineage 1000", _manufacturerId)).Wait();
            _client!.AddResponse(FullResponse);
            _client!.AddResponse(AirlineResponse);
            var details = Task.Run(() => _manager!.LookupAircraft(AircraftAddress)).Result;

            Assert.IsNotNull(details);
            Assert.IsNotNull(details.Airline);
            Assert.IsNotNull(details.Model);
            Assert.AreEqual(AircraftAddress, details.Address);
            Assert.AreEqual("LH", details.Airline.IATA);
            Assert.AreEqual("DLH", details.Airline.ICAO);
            Assert.AreEqual("Lufthansa", details.Airline.Name);
            Assert.AreEqual("E90", details.Model.IATA);
            Assert.AreEqual("E190", details.Model.ICAO);
            Assert.AreEqual("190 / Lineage 1000", details.Model.Name);
            Assert.AreEqual("Embraer", details.Model.Manufacturer.Name);
        }

        [TestMethod]
        public void AirlineWithICAOOnlyNotInDatabaseTest()
        {
            Task.Run(() => _models!.AddAsync("E90", "E190", "190 / Lineage 1000", _manufacturerId)).Wait();
            _client!.AddResponse(AirlineWithNoIATAResponse);
            _client!.AddResponse(AirlineResponse);
            var details = Task.Run(() => _manager!.LookupAircraft(AircraftAddress)).Result;

            Assert.IsNotNull(details);
            Assert.IsNotNull(details.Airline);
            Assert.IsNotNull(details.Model);
            Assert.AreEqual(AircraftAddress, details.Address);
            Assert.AreEqual("LH", details.Airline.IATA);
            Assert.AreEqual("DLH", details.Airline.ICAO);
            Assert.AreEqual("Lufthansa", details.Airline.Name);
            Assert.AreEqual("E90", details.Model.IATA);
            Assert.AreEqual("E190", details.Model.ICAO);
            Assert.AreEqual("190 / Lineage 1000", details.Model.Name);
            Assert.AreEqual("Embraer", details.Model.Manufacturer.Name);
        }

        [TestMethod]
        public void AirlineIsInDatabaseTest()
        {
            Task.Run(() => _airlines!.AddAsync("LH", "DLH", "Lufthansa")).Wait();
            Task.Run(() => _models!.AddAsync("E90", "E190", "190 / Lineage 1000", _manufacturerId)).Wait();
            _client!.AddResponse(FullResponse);
            _client!.AddResponse(AirlineResponse);
            var details = Task.Run(() => _manager!.LookupAircraft(AircraftAddress)).Result;

            Assert.IsNotNull(details);
            Assert.IsNotNull(details.Airline);
            Assert.IsNotNull(details.Model);
            Assert.AreEqual(AircraftAddress, details.Address);
            Assert.AreEqual("LH", details.Airline.IATA);
            Assert.AreEqual("DLH", details.Airline.ICAO);
            Assert.AreEqual("Lufthansa", details.Airline.Name);
            Assert.AreEqual("E90", details.Model.IATA);
            Assert.AreEqual("E190", details.Model.ICAO);
            Assert.AreEqual("190 / Lineage 1000", details.Model.Name);
            Assert.AreEqual("Embraer", details.Model.Manufacturer.Name);
        }

        [TestMethod]
        public void DetailsAreInDatabaseTest()
        {
            var airlineId = Task.Run(() => _airlines!.AddAsync("LH", "DLH", "Lufthansa")).Result.Id;
            var modelId = Task.Run(() => _models!.AddAsync("E90", "E190", "190 / Lineage 1000", _manufacturerId)).Result.Id;
            Task.Run(() => _details!.AddAsync(AircraftAddress, airlineId, modelId)).Wait();
            var details = Task.Run(() => _manager!.LookupAircraft(AircraftAddress)).Result;

            Assert.IsNotNull(details);
            Assert.IsNotNull(details.Airline);
            Assert.IsNotNull(details.Model);
            Assert.AreEqual(AircraftAddress, details.Address);
            Assert.AreEqual("LH", details.Airline.IATA);
            Assert.AreEqual("DLH", details.Airline.ICAO);
            Assert.AreEqual("Lufthansa", details.Airline.Name);
            Assert.AreEqual("E90", details.Model.IATA);
            Assert.AreEqual("E190", details.Model.ICAO);
            Assert.AreEqual("190 / Lineage 1000", details.Model.Name);
            Assert.AreEqual("Embraer", details.Model.Manufacturer.Name);
        }
    }
}
