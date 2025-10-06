using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class AirlineConstantsImportHandler : CommandHandlerBase
    {
        public AirlineConstantsImportHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory) : base (settings, parser, logger, factory)
        {

        }

        /// <summary>
        /// Handle the numer/suffix rule import command
        /// </summary>
        /// <returns></returns>
        public async Task Handle()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportAirlineConstants)[0];
            var importer = new AirlineConstantsImporter(Factory.AirlineConstantsManager, Logger);
            await importer.Truncate();
            await importer.Import(filePath);
        }
    }
}