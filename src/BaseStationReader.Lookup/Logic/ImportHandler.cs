using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.Lookup.Logic
{
    internal class ImportHandler : CommandHandlerBase
    {
        public ImportHandler(
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
        public async Task HandleAircraftImportAsync()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportAircraft)[0];
            var importer = new AircraftImporter(Factory);
            await importer.ImportAsync(filePath);
        }

        /// <summary>
        /// Handle the airline import command
        /// </summary>
        /// <returns></returns>
        public async Task HandleAirlineImportAsync()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportAirlines)[0];
            var airlineImporter = new AirlineImporter(Factory);
            await airlineImporter.ImportAsync(filePath);
        }

        /// <summary>
        /// Handle the callsign/flight IATA code mapping import command
        /// </summary>
        /// <returns></returns>
        public async Task HandleMappingImportAsync()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportFlightIATACodeMappings)[0];
            var importer = new FlightIATACodeMappingImporter(Factory);
            await importer.ImportAsync(filePath);
        }

        /// <summary>
        /// Handle the manufacturer import command
        /// </summary>
        /// <returns></returns>
        public async Task HandleManufacturerImportAsync()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportManufacturers)[0];
            var manufacturerImporter = new ManufacturerImporter(Factory);
            await manufacturerImporter.ImportAsync(filePath);
        }

        /// <summary>
        /// Handle the model import command
        /// </summary>
        /// <returns></returns>
        public async Task HandleModelImportAsync()
        {
            var filePath = Parser.GetValues(CommandLineOptionType.ImportModels)[0];
            var modelImporter = new ModelImporter(Factory);
            await modelImporter.ImportAsync(filePath);
        }
    }
}