using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;

namespace BaseStationReader.BusinessLogic.Import
{
    public class ModelImporter : CsvImporter<ModelMappingProfile, Model>, IModelImporter
    {
        private readonly IDatabaseManagementFactory _factory;

        public ModelImporter(IDatabaseManagementFactory factory) : base(factory.Logger)
            => _factory = factory;

        /// <summary>
        /// Read a set of model instances from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public override List<Model> Read(string filePath)
        {
            // Load the data
            var models = base.Read(filePath);
            if (models?.Count > 0)
            {
                // Clean up the model codes
                foreach (var model in models.Where(x => Replacements.Contains(x.IATA)))
                {
                    model.IATA = null;
                }

                foreach (var model in models.Where(x => Replacements.Contains(x.ICAO)))
                {
                    model.ICAO = null;
                }

                foreach (var model in models)
                {
                    if (string.IsNullOrWhiteSpace(model.IATA))
                    {
                        model.IATA = null;
                    }
                    if (string.IsNullOrWhiteSpace(model.ICAO))
                    {
                        model.ICAO = null;
                    }
                }

                // Identify instances where there's no IATA or ICAO code and remove them
                models.RemoveAll(x => string.IsNullOrEmpty(x.ICAO) && string.IsNullOrEmpty(x.IATA));
                Logger.LogMessage(Severity.Info, $"Models with no IATA/ICAO code removed : {models.Count} models remaining");

            }

            return models;
        }

        /// <summary>
        /// Save a collection of models to the database
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public override async Task SaveAsync(IEnumerable<Model> models)
        {
            if (models?.Any() == true)
            {
                // Resolve manufacturer references asynchronously as part of the asynchronous import workflow.
                var manufacturers = await _factory.ManufacturerManager.ListAsync(x => true);
                foreach (var model in models)
                {
                    var manufacturer = manufacturers.FirstOrDefault(x =>
                        x.Name.Equals(model.ManufacturerName, StringComparison.OrdinalIgnoreCase));
                    model.ManufacturerId = manufacturer?.Id ?? 0;
                }

                models = models.Where(model => model.ManufacturerId > 0).ToList();
                Logger.LogMessage(Severity.Info, $"Saving {models.Count()} models to the database");

                var provenanceRecords = await _factory.ProvenanceManager.ListAsync(x => true);
                var provenanceByRef = provenanceRecords.ToDictionary(x => x.SourceRef, StringComparer.Ordinal);
                var missing = models
                    .Select(x => x.ProvenanceRef?.Trim() ?? "")
                    .Distinct(StringComparer.Ordinal)
                    .Where(sourceRef => !provenanceByRef.ContainsKey(sourceRef))
                    .ToList();

                if (missing.Count > 0)
                {
                    throw new InvalidOperationException($"Provenance record(s) not found: {string.Join(", ", missing)}");
                }

                foreach (var model in models)
                {
                    var provenance = provenanceByRef[model.ProvenanceRef.Trim()];
                    Logger.LogMessage(Severity.Debug, $"Saving model '{model.Name}' : IATA = '{model.IATA}', ICAO = '{model.ICAO}'");
                    await _factory.ModelManager.AddAsync(model.IATA, model.ICAO, model.Name, model.ManufacturerId, provenance.Id);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, $"No models to save");
            }
        }
    }
}
