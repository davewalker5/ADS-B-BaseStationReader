using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class AircraftImportHandler : CommandHandlerBase
    {
        public AircraftImportHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory) : base (settings, parser, logger, factory)
        {

        }

        /// <summary>
        /// Handle the Aircraft import command
        /// </summary>
        /// <returns></returns>
        public async Task HandleAsync()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportAircraft)[0];
            var AircraftImporter = new AircraftImporter(Factory.AircraftManager, Factory.ModelManager, Logger);
            await AircraftImporter.ImportAsync(filePath);
        }
    }
}