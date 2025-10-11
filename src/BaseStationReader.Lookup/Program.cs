using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Database;
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
        public static async Task Main(string[] args)
        {
            // Process the command line arguments
            var parser = new LookupToolCommandLineParser(new HelpTabulator());
            parser.Parse(args);

            // If help's been requested, show help and exit
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
                logger.Initialise(settings.LogFile, settings.MinimumLogLevel, false);

                // Get the version number and application title
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
                var title = $"Aircraft Lookup Tool v{info.FileVersion}";

                // Show the startup messages
                Console.WriteLine(new string('=', 80));
                Console.WriteLine(title);
                Console.WriteLine($"Output will be logged to {settings.LogFile}");

                // Log the startup messages
                logger.LogMessage(Severity.Info, new string('=', 80));
                logger.LogMessage(Severity.Info, title);

                // Make sure the latest migrations have been applied - this ensures the DB is created and in the
                // correct state if it's absent or stale on startup
                var context = new BaseStationReaderDbContextFactory().CreateDbContext([]);
                context.Database.Migrate();
                logger.LogMessage(Severity.Debug, "Latest database migrations have been applied");

                // Create the database management factory
                var factory = new DatabaseManagementFactory(logger, context, 0, settings.MaximumLookups);

                // If a CSV file containing airline details has been supplied, import it
                if (parser.IsPresent(CommandLineOptionType.ImportAirlines))
                {
                    await new AirlineImportHandler(settings, parser, logger, factory).HandleAsync();
                }

                // If a CSV file containing manufacturer details has been supplied, import it
                if (parser.IsPresent(CommandLineOptionType.ImportManufacturers))
                {
                    await new ManufacturerImportHandler(settings, parser, logger, factory).HandleAsync();
                }

                // If a CSV file containing confirmed flight number mappings has been supplied, import it
                if (parser.IsPresent(CommandLineOptionType.ImportFlightNumberMappings))
                {
                    await new FlightNumberMappingImportHandler(settings, parser, logger, factory).HandleAsync();
                }

                // If a CSV file containing model details has been supplied, import it
                if (parser.IsPresent(CommandLineOptionType.ImportModels))
                {
                    await new ModelImportHandler(settings, parser, logger, factory).HandleAsync();
                }

                // If an aircraft address has been supplied, look it up and store the results
                if (parser.IsPresent(CommandLineOptionType.AircraftAddress))
                {
                    var serviceType = ExternalApiFactory.GetServiceTypeFromString(settings.LiveApi);
                    await new AircraftLookupHandler(settings, parser, logger, factory, serviceType).HandleAsync();
                }

                // Lookup historical flight details and store the results
                if (parser.IsPresent(CommandLineOptionType.HistoricalLookup))
                {
                    var serviceType = ExternalApiFactory.GetServiceTypeFromString(settings.HistoricalApi);
                    await new HistoricalAircraftLookupHandler(settings, parser, logger, factory, serviceType).HandleAsync();
                }

                // Look up live flights within a given bounding box of the receiver
                if (parser.IsPresent(CommandLineOptionType.FlightsInRange))
                {
                    var serviceType = ExternalApiFactory.GetServiceTypeFromString(settings.LiveApi);
                    await new FlightsInRangeHandler(settings, parser, logger, factory, serviceType).HandleAsync();
                }

                // Look up the current weather at a given airport
                if (parser.IsPresent(CommandLineOptionType.METAR))
                {
                    var serviceType = ExternalApiFactory.GetServiceTypeFromString(settings.WeatherApi);
                    await new AirportWeatherLookupHandler(settings, parser, logger, factory, serviceType).HandleMetarAsync();
                }

                // Look up the weather forecast at a given airport
                if (parser.IsPresent(CommandLineOptionType.TAF))
                {
                    var serviceType = ExternalApiFactory.GetServiceTypeFromString(settings.WeatherApi);
                    await new AirportWeatherLookupHandler(settings, parser, logger, factory, serviceType).HandleTafAsync();
                }

                // Export schedule information for a specified airport and, optionally, date range
                if (parser.IsPresent(CommandLineOptionType.ExportSchedule))
                {
                    await new ScheduleLookupHandler(settings, parser, logger, factory, ApiServiceType.AeroDataBox).HandleAsync();
                }
            }
        }
    }
}