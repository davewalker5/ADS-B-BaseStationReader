using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class ManufacturerImportHandler : CommandHandlerBase
    {
        public ManufacturerImportHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            BaseStationReaderDbContext context) : base (settings, parser, logger, context)
        {

        }

        /// <summary>
        /// Handle the manufacturer import command
        /// </summary>
        /// <returns></returns>
        public async Task Handle()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportManufacturers)[0];
            var manufacturerManager = new ManufacturerManager(Context);
            var manufacturerImporter = new ManufacturerImporter(manufacturerManager, Logger);
            await manufacturerImporter.Import(filePath);
        }
    }
}