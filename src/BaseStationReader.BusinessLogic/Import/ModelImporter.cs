using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.BusinessLogic.Logging
{
    public class ModelImporter : CsvImporter<ModelMappingProfile, Model>, IModelImporter
    {
        private readonly IManufacturerManager _manufacturerManager;
        private readonly IModelManager _modelManager;

        public ModelImporter(IManufacturerManager manufacturerManager, IModelManager modelManager, ITrackerLogger logger) : base(logger)
        {
            _manufacturerManager = manufacturerManager;
            _modelManager = modelManager;
        }

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
                    model.IATA = "";
                }

                foreach (var model in models.Where(x => Replacements.Contains(x.ICAO)))
                {
                    model.ICAO = "";
                }

                // Identify instances where there's no IATA or ICAO code and remove them
                models.RemoveAll(x => string.IsNullOrEmpty(x.ICAO) && string.IsNullOrEmpty(x.IATA));
                Logger.LogMessage(Severity.Info, $"Models with no IATA/ICAO code removed : {models.Count} models remaining");

                // Populate the manufacturer ID on each model
                var manufacturers = Task.Run(() => _manufacturerManager.ListAsync(x => true)).Result;
                foreach (var model in models)
                {
                    var manufacturer = manufacturers.FirstOrDefault(x => x.Name.Equals(model.ManufacturerName, StringComparison.OrdinalIgnoreCase));
                    model.ManufacturerId = manufacturer != null ? manufacturer.Id : 0;
                }

                // Remove any models for which the manufacturer hasn't been identified
                models.RemoveAll(x => x.ManufacturerId == 0);
                Logger.LogMessage(Severity.Info, $"Models with no manufacturer removed : {models.Count} models remaining");
            }

            return models;
        }

        /// <summary>
        /// Save a collection of models to the database
        /// </summary>
        /// <param name="models"></param>
        /// <returns></returns>
        public override async Task Save(IEnumerable<Model> models)
        {
            if (models?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {models.Count()} models to the database");

                foreach (var model in models)
                {
                    Logger.LogMessage(Severity.Debug, $"Saving model '{model.Name}' : IATA = '{model.IATA}', ICAO = '{model.ICAO}'");
                    await _modelManager.AddAsync(model.IATA, model.ICAO, model.Name, model.ManufacturerId);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, $"No models to save");
            }
        }
    }
}