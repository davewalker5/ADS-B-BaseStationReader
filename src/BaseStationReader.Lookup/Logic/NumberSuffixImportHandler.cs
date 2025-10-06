using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class NumberSuffixImportHandler : CommandHandlerBase
    {
        public NumberSuffixImportHandler(
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
            var filePath = Parser.GetValues(CommandLineOptionType.ImportNumberSuffixRules)[0];
            var numberSuffixImporter = new NumberSuffixImporter(Factory.NumberSuffixManager, Logger);
            await numberSuffixImporter.Truncate();
            await numberSuffixImporter.Import(filePath);
        }
    }
}