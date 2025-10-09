using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class ModelImportHandler : CommandHandlerBase
    {
        public ModelImportHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            DatabaseManagementFactory factory) : base (settings, parser, logger, factory)
        {

        }

        /// <summary>
        /// Handle the model import command
        /// </summary>
        /// <returns></returns>
        public async Task HandleAsync()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportModels)[0];
            var modelImporter = new ModelImporter(Factory.ManufacturerManager, Factory.ModelManager, Logger);
            await modelImporter.Import(filePath);
        }
    }
}