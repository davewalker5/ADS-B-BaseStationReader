using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class FlightNumberMappingImportHandler : CommandHandlerBase
    {
        public FlightNumberMappingImportHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory) : base (settings, parser, logger, factory)
        {

        }

        /// <summary>
        /// Handle the confirmed flight number mapping import command
        /// </summary>
        /// <returns></returns>
        public async Task Handle()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportFlightNumberMappings)[0];
            var importer = new FlightNumberMappingImporter(Factory.ConfirmedMappingManager, Logger);
            await importer.Import(filePath);
        }
    }
}