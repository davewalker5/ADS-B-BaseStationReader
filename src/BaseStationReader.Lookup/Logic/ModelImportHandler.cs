using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.Lookup.Logic
{
    internal class ModelImportHandler : CommandHandlerBase
    {
        public ModelImportHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            BaseStationReaderDbContext context) : base (settings, parser, logger, context)
        {

        }

        /// <summary>
        /// Handle the model import command
        /// </summary>
        /// <returns></returns>
        public override async Task Handle()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportModels)[0];
            var manufacturerManager = new ManufacturerManager(Context);
            var modelManager = new ModelManager(Context);
            var modelImporter = new ModelImporter(manufacturerManager, modelManager, Logger);
            await modelImporter.Import(filePath);
        }
    }
}