using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class SuffixDeltaRuleImportHandler : CommandHandlerBase
    {
        public SuffixDeltaRuleImportHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory) : base (settings, parser, logger, factory)
        {

        }

        /// <summary>
        /// Handle the suffix delta rule import command
        /// </summary>
        /// <returns></returns>
        public async Task Handle()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportSuffixDeltaRules)[0];
            var numberSuffixImporter = new SuffixDeltaRuleImporter(Factory.SuffixDeltaRuleManager, Logger);
            await numberSuffixImporter.Truncate();
            await numberSuffixImporter.Import(filePath);
        }
    }
}