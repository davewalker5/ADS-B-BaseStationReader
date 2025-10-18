using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;

namespace BaseStationReader.BusinessLogic.Logging
{
    public class ManufacturerImporter : CsvImporter<ManufacturerMappingProfile, Manufacturer>, IManufacturerImporter
    {
        private readonly IDatabaseManagementFactory _factory;

        public ManufacturerImporter(IDatabaseManagementFactory factory) : base(factory.Logger)
            => _factory = factory;

        /// <summary>
        /// Read a set of airline instances from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public override List<Manufacturer> Read(string filePath)
        {
            // Load the data
            var manufacturers = base.Read(filePath);
            if (manufacturers?.Count > 0)
            {
                // Make the list distinct
                manufacturers = [.. manufacturers.DistinctBy(x => x.Name)];
                Logger.LogMessage(Severity.Info, $"{manufacturers.Count} distinct manufacturers remaining");
            }

            return manufacturers;
        }

        /// <summary>
        /// Save a collection of manufacturers to the database
        /// </summary>
        /// <param name="manufacturers"></param>
        /// <returns></returns>
        public override async Task SaveAsync(IEnumerable<Manufacturer> manufacturers)
        {
            if (manufacturers?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {manufacturers.Count()} manufacturers to the database");

                foreach (var manufacturer in manufacturers)
                {
                    Logger.LogMessage(Severity.Debug, $"Saving manufacturer '{manufacturer.Name}'");
                    await _factory.ManufacturerManager.AddAsync(manufacturer.Name);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, $"No manufacturers to save");
            }
        }
    }
}