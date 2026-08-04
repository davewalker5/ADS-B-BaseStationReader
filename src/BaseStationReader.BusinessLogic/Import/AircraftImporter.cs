using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;

namespace BaseStationReader.BusinessLogic.Import
{
    public class AircraftImporter : CsvImporter<AircraftMappingProfile, Aircraft>, IAircraftImporter
    {
        private readonly IDatabaseManagementFactory _factory;

        public AircraftImporter(IDatabaseManagementFactory factory) : base(factory.Logger)
            => _factory = factory;

        /// <summary>
        /// Read a set of Aircraft instances from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public override List<Aircraft> Read(string filePath)
        {
            // Load the data
            var aircraft = base.Read(filePath);
            if (aircraft?.Count > 0)
            {
                // Identify instances where there's no address and remove them
                aircraft.RemoveAll(x => string.IsNullOrEmpty(x.Address));
                Logger.LogMessage(Severity.Info, $"Aircraft with no address removed : {aircraft.Count} aircraft remaining");

                // Identify instances where there's no registration and remove them
                aircraft.RemoveAll(x => string.IsNullOrEmpty(x.Registration));
                Logger.LogMessage(Severity.Info, $"Aircraft with no registration removed : {aircraft.Count} aircraft remaining");

                // Identify instances where there's no model IATA or ICAO code and remove them
                aircraft.RemoveAll(x => string.IsNullOrEmpty(x.ModelICAO) && string.IsNullOrEmpty(x.ModelIATA));
                Logger.LogMessage(Severity.Info, $"Aircraft with no model IATA/ICAO code removed : {aircraft.Count} aircraft remaining");

            }

            return aircraft;
        }

        /// <summary>
        /// Save a collection of aircraft to the database
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public override async Task SaveAsync(IEnumerable<Aircraft> aircraft)
        {
            if (aircraft?.Any() == true)
            {
                // Resolve model references asynchronously as part of the asynchronous import workflow.
                foreach (var item in aircraft)
                {
                    var model = await _factory.ModelManager.GetAsync(
                        item.ModelIATA,
                        item.ModelICAO,
                        item.Model?.Name);
                    item.ModelId = model?.Id ?? 0;
                }

                aircraft = aircraft.Where(item => item.ModelId > 0).ToList();
                Logger.LogMessage(Severity.Info, $"Saving {aircraft.Count()} aircraft to the database");

                var provenanceRecords = await _factory.ProvenanceManager.ListAsync(x => true);
                var provenanceByRef = provenanceRecords.ToDictionary(x => x.SourceRef, StringComparer.Ordinal);
                var missing = aircraft
                    .Select(x => x.ProvenanceRef?.Trim() ?? "")
                    .Distinct(StringComparer.Ordinal)
                    .Where(sourceRef => !provenanceByRef.ContainsKey(sourceRef))
                    .ToList();

                if (missing.Count > 0)
                {
                    throw new InvalidOperationException($"Provenance record(s) not found: {string.Join(", ", missing)}");
                }

                foreach (var a in aircraft)
                {
                    var provenance = provenanceByRef[a.ProvenanceRef.Trim()];
                    Logger.LogMessage(Severity.Debug, $"Saving Aircraft '{a.Address}' : " +
                        $"Registration = '{a.Registration}', " +
                        $"Model ICAO = '{a.ModelICAO}', " +
                        $"Model IATA = '{a.ModelIATA}', " +
                        $"Manufactured = {a.Manufactured}");

                    var age = a.Manufactured > 0 ? DateTime.Today.Year - a.Manufactured : null;
                    await _factory.AircraftManager.AddAsync(a.Address, a.Registration, a.Manufactured, age, a.ModelId, provenance.Id);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, $"No aircraft to save");
            }
        }
    }
}
