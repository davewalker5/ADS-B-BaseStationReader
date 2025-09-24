using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Lookup.Logic;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Reflection;

namespace BaseStationReader.Lookup
{
    public static class Program
    {
        private static readonly FileLogger _logger = new();
        private static readonly LookupToolCommandLineParser _parser = new(new HelpTabulator());

        public static async Task Main(string[] args)
        {
            // Process the command line arguments. If help's been requested, show help and exit
            _parser.Parse(args);
            if (_parser.IsPresent(CommandLineOptionType.Help))
            {
                _parser.Help();
            }
            else
            {
                // Read the application config file
                var settings = new LookupToolSettingsBuilder().BuildSettings(_parser, "appsettings.json");

                // Configure the log file
                _logger.Initialise(settings.LogFile, settings.MinimumLogLevel);

                // Get the version number and application title
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
                var title = $"Aircraft Lookup Tool v{info.FileVersion}";

                // Show the startup messages
                Console.WriteLine(new string('=', 80));
                Console.WriteLine(title);
                Console.WriteLine($"Output will be logged to {settings.LogFile}");

                // Log the startup messages
                _logger.LogMessage(Severity.Info, new string('=', 80));
                _logger.LogMessage(Severity.Info, title);

                // Make sure the latest migrations have been applied - this ensures the DB is created and in the
                // correct state if it's absent or stale on startup
                var context = new BaseStationReaderDbContextFactory().CreateDbContext([]);
                context.Database.Migrate();
                _logger.LogMessage(Severity.Debug, "Latest database migrations have been applied");

                // If a CSV file containing airline details has been supplied, import it
                if (_parser.IsPresent(CommandLineOptionType.ImportAirlines))
                {
                    await new AirlineImportHandler(settings, _parser, _logger, context).Handle();
                }

                // If a CSV file containing manufacturer details has been supplied, import it
                if (_parser.IsPresent(CommandLineOptionType.ImportManufacturers))
                {
                    await new ManufacturerImportHandler(settings, _parser, _logger, context).Handle();
                }

                // If a CSV file containing model details has been supplied, import it
                if (_parser.IsPresent(CommandLineOptionType.ImportModels))
                {
                    await new ModelImportHandler(settings, _parser, _logger, context).Handle();
                }

                // If an aircraft address has been supplied, look it up and store the results
                if (_parser.IsPresent(CommandLineOptionType.AircraftAddress))
                {
                    await new AircraftLookupHandler(settings, _parser, _logger, context).Handle();
                }

                if (_parser.IsPresent(CommandLineOptionType.FlightsInRange))
                {
                    await new FlightsInRangeHandler(settings, _parser, _logger, context).Handle();
                }
            }
        }
    }
}