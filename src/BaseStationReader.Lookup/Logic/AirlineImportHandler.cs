using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class AirlineImportHandler : CommandHandlerBase
    {
        public AirlineImportHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            BaseStationReaderDbContext context) : base (settings, parser, logger, context)
        {

        }

        /// <summary>
        /// Handle the airline import command
        /// </summary>
        /// <returns></returns>
        public override async Task Handle()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportAirlines)[0];
            var airlineManager = new AirlineManager(Context);
            var airlineImporter = new AirlineImporter(airlineManager, Logger);
            await airlineImporter.Import(filePath);
        }
    }
}