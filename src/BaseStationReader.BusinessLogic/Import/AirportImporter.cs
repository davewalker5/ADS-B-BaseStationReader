using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;

namespace BaseStationReader.BusinessLogic.Import
{
    public class AirportImporter : CsvImporter<AirportMappingProfile, Airport>, IAirportImporter
    {
        private readonly IDatabaseManagementFactory _factory;

        /// <summary>
        /// Initialise an airport importer using the supplied database management factory.
        /// </summary>
        public AirportImporter(IDatabaseManagementFactory factory) : base(factory.Logger)
            => _factory = factory;

        /// <summary>
        /// Read airport instances from a CSV file.
        /// </summary>
        public override List<Airport> Read(string filePath)
        {
            var airports = base.Read(filePath);
            if (airports?.Count > 0)
            {
                // Treat the source file's placeholder code values as missing values.
                foreach (var airport in airports.Where(x => Replacements.Contains(x.IATA)))
                {
                    airport.IATA = "";
                }

                foreach (var airport in airports.Where(x => Replacements.Contains(x.ICAO)))
                {
                    airport.ICAO = "";
                }

                airports.RemoveAll(x => string.IsNullOrEmpty(x.ICAO) && string.IsNullOrEmpty(x.IATA));
                Logger.LogMessage(Severity.Info, $"Airports with no IATA/ICAO code removed : {airports.Count} airports remaining");
            }

            return airports;
        }

        /// <summary>
        /// Save a collection of airports to the database.
        /// </summary>
        public override async Task SaveAsync(IEnumerable<Airport> airports)
        {
            if (airports?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {airports.Count()} airports to the database");

                var provenanceRecords = await _factory.ProvenanceManager.ListAsync(x => true);
                var provenanceByRef = provenanceRecords.ToDictionary(x => x.SourceRef, StringComparer.Ordinal);
                var missing = airports
                    .Select(x => x.ProvenanceRef?.Trim() ?? "")
                    .Distinct(StringComparer.Ordinal)
                    .Where(sourceRef => !provenanceByRef.ContainsKey(sourceRef))
                    .ToList();

                if (missing.Count > 0)
                {
                    throw new InvalidOperationException($"Provenance record(s) not found: {string.Join(", ", missing)}");
                }

                foreach (var airport in airports)
                {
                    var provenance = provenanceByRef[airport.ProvenanceRef.Trim()];
                    airport.ProvenanceId = provenance.Id;
                    airport.Provenance = provenance;
                    Logger.LogMessage(Severity.Debug, $"Saving airport '{airport.Name}' : IATA = '{airport.IATA}', ICAO = '{airport.ICAO}'");
                    await _factory.AirportManager.AddAsync(airport);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, "No airports to save");
            }
        }
    }
}
