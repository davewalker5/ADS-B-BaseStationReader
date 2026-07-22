using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Tests.Mocks;

namespace BaseStationReader.Tests.DataExchange
{
    [TestClass]
    public class FlightImporterTest
    {
        private IDatabaseManagementFactory _factory = null!;
        private IFlightImporter _importer = null!;

        [TestInitialize]
        public async Task InitialiseAsync()
        {
            var context = BaseStationReaderDbContextFactory.CreateInMemoryDbContext();
            _factory = new DatabaseManagementFactory(new MockFileLogger(), context, 0);
            _importer = new FlightImporter(_factory);

            var provenance = await _factory.ProvenanceManager.AddAsync(
                "TEST", "Test source", "N/A", "Flights", "1", "N/A");
            await _factory.AirportManager.AddAsync(new Airport
            {
                ICAO = "EGLL", IATA = "LHR", Name = "London Heathrow", ProvenanceId = provenance.Id
            });
            await _factory.AirportManager.AddAsync(new Airport
            {
                ICAO = "KEWR", IATA = "EWR", Name = "Newark", ProvenanceId = provenance.Id
            });
            await _factory.AirlineManager.AddAsync("BA", "BAW", "British Airways", provenance.Id);
            await _factory.AirlineManager.AddAsync("UA", "UAL", "United Airlines", provenance.Id);
        }

        [TestMethod]
        public async Task ImportResolvesReferencesTestAsync()
        {
            await _importer.ImportAsync("flights.csv");
            var flights = await _factory.FlightManager.ListAsync(x => true);

            Assert.HasCount(3, flights);

            var icaoFlight = flights.Single(x => x.Callsign == "BAW185A");
            Assert.AreEqual("BA185", icaoFlight.IATA);
            Assert.AreEqual("BAW185", icaoFlight.ICAO);
            Assert.AreEqual("EGLL", icaoFlight.OriginAirport?.ICAO);
            Assert.AreEqual("KEWR", icaoFlight.DestinationAirport?.ICAO);
            Assert.AreEqual("BAW", icaoFlight.Airline?.ICAO);
            Assert.AreEqual("TEST", icaoFlight.Provenance.SourceRef);

            var iataFlight = flights.Single(x => x.Callsign == "UAL900");
            Assert.AreEqual("LHR", iataFlight.OriginAirport?.IATA);
            Assert.AreEqual("EWR", iataFlight.DestinationAirport?.IATA);
            Assert.AreEqual("UA", iataFlight.Airline?.IATA);

            var optionalIcaoFlight = flights.Single(x => x.Callsign == "TEST123");
            Assert.AreEqual("T123", optionalIcaoFlight.IATA);
            Assert.AreEqual(string.Empty, optionalIcaoFlight.ICAO);
        }

        [TestMethod]
        public async Task ImportFailsWhenReferenceIsMissingTestAsync()
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _importer.SaveAsync(
            [
                new Flight { Callsign = "BAD123", IATA = "B123", ICAO = "BAD123", OriginICAO = "XXXX", ProvenanceRef = "TEST" }
            ]));

            Assert.IsEmpty(await _factory.FlightManager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task ImportFailsWhenProvenanceIsMissingTestAsync()
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _importer.SaveAsync(
            [
                new Flight { Callsign = "BAD123", IATA = "B123", ICAO = "BAD123", ProvenanceRef = "MISSING" }
            ]));

            Assert.IsEmpty(await _factory.FlightManager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task ImportFailsWhenRequiredRelationshipIsMissingTestAsync()
        {
            await Assert.ThrowsExactlyAsync<InvalidOperationException>(() => _importer.SaveAsync(
            [
                new Flight { Callsign = "BAD123", IATA = "B123", ICAO = "BAD123", ProvenanceRef = "TEST" }
            ]));

            Assert.IsEmpty(await _factory.FlightManager.ListAsync(x => true));
        }

        [TestMethod]
        public async Task ImportEmptyFileTestAsync()
        {
            await _importer.ImportAsync("empty_flights.csv");
            Assert.IsEmpty(await _factory.FlightManager.ListAsync(x => true));
        }
    }
}
