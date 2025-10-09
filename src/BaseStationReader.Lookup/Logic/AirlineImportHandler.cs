using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class AirlineImportHandler : CommandHandlerBase
    {
        public AirlineImportHandler(
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
        public async Task HandleAsync()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportAirlines)[0];
            var airlineImporter = new AirlineImporter(Factory.AirlineManager, Logger);
            await airlineImporter.Import(filePath);
        }
    }
}