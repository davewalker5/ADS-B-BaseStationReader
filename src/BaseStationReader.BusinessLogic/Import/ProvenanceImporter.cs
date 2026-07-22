using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;

namespace BaseStationReader.BusinessLogic.Logging
{
    public class ProvenanceImporter : CsvImporter<ProvenanceMappingProfile, Provenance>, IProvenanceImporter
    {
        private readonly IDatabaseManagementFactory _factory;

        public ProvenanceImporter(IDatabaseManagementFactory factory) : base(factory.Logger)
            => _factory = factory;

        public override List<Provenance> Read(string filePath)
        {
            var records = base.Read(filePath);
            if (records?.Count > 0)
            {
                records = [.. records.DistinctBy(x => x.SourceRef)];
                Logger.LogMessage(Severity.Info, $"{records.Count} distinct provenance records remaining");
            }

            return records;
        }

        public override async Task SaveAsync(IEnumerable<Provenance> records)
        {
            if (records?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {records.Count()} provenance records to the database");
                foreach (var record in records)
                {
                    Logger.LogMessage(Severity.Debug, $"Saving provenance record '{record.SourceRef}'");
                    await _factory.ProvenanceManager.AddAsync(record.SourceRef, record.Source,
                        record.SourceUrl, record.SourceDataset, record.SourceVersion, record.Licence);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, "No provenance records to save");
            }
        }
    }
}
