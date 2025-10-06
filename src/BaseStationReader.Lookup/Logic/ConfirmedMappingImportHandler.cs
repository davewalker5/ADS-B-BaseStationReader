using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class ConfirmedMappingImportHandler : CommandHandlerBase
    {
        public ConfirmedMappingImportHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory) : base (settings, parser, logger, factory)
        {

        }

        /// <summary>
        /// Handle the airline import command
        /// </summary>
        /// <returns></returns>
        public async Task Handle()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportConfirmedMappings)[0];
            var confirmedMappingImporter = new ConfirmedMappingImporter(Factory.ConfirmedMappingManager, Logger);
            await confirmedMappingImporter.Truncate();
            await confirmedMappingImporter.Import(filePath);
        }
    }
}