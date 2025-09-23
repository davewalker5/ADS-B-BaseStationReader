using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Lookup.Logic;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BaseStationReader.Lookup
{
    public static class Program
    {
        public static async Task Main(string[] args)
        {
            // Process the command line arguments. If help's been requested, show help and exit
            var parser = new LookupToolCommandLineParser(new HelpTabulator());
            parser.Parse(args);
            if (parser.IsPresent(CommandLineOptionType.Help))
            {
                parser.Help();
            }
            else
            {
                // Read the application config file
                var settings = new LookupToolSettingsBuilder().BuildSettings(parser, "appsettings.json");

                // Configure the log file
                var logger = new FileLogger();
                logger.Initialise(settings.LogFile, settings.MinimumLogLevel);

                // Get the version number and application title
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
                var title = $"Aircraft Lookup Tool v{info.FileVersion}";

                // Log the startup messages
                logger.LogMessage(Severity.Info, new string('=', 80));
                logger.LogMessage(Severity.Info, title);

                // Make sure the latest migrations have been applied - this ensures the DB is created and in the
                // correct state if it's absent or stale on startup
                var context = new BaseStationReaderDbContextFactory().CreateDbContext([]);
                context.Database.Migrate();
                logger.LogMessage(Severity.Debug, "Latest database migrations have been applied");

                // If a CSV file containing airline details has been supplied, import it
                if (parser.IsPresent(CommandLineOptionType.ImportAirlines))
                {
                    var filePath = parser.GetValues(CommandLineOptionType.ImportAirlines)[0];
                    var airlineManager = new AirlineManager(context);
                    var airlineImporter = new AirlineImporter(airlineManager, logger);
                    await airlineImporter.Import(filePath);
                }

                // If a CSV file containing manufacturer details has been supplied, import it
                if (parser.IsPresent(CommandLineOptionType.ImportManufacturers))
                {
                    var filePath = parser.GetValues(CommandLineOptionType.ImportManufacturers)[0];
                    var manufacturerManager = new ManufacturerManager(context);
                    var manufacturerImporter = new ManufacturerImporter(manufacturerManager, logger);
                    await manufacturerImporter.Import(filePath);
                }

                // If a CSV file containing model details has been supplied, import it
                if (parser.IsPresent(CommandLineOptionType.ImportModels))
                {
                    var filePath = parser.GetValues(CommandLineOptionType.ImportModels)[0];
                    var manufacturerManager = new ManufacturerManager(context);
                    var modelManager = new ModelManager(context);
                    var modelImporter = new ModelImporter(manufacturerManager, modelManager, logger);
                    await modelImporter.Import(filePath);
                }

                // If an aircraft address has been supplied, look it up and store the results
                if (parser.IsPresent(CommandLineOptionType.AircraftAddress))
                {
                    var lookupManager = new LookupManager(settings, logger, context, parser);
                    await lookupManager.Lookup();
                }
            }
        }
    }
}