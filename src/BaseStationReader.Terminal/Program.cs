using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.BusinessLogic.Tracking;
using BaseStationReader.Terminal.Interfaces;
using BaseStationReader.Terminal.Logic;
using Microsoft.EntityFrameworkCore;
using Spectre.Console;
using System.Diagnostics;
using System.Reflection;
using BaseStationReader.Data;
using BaseStationReader.BusinessLogic.Api;

namespace BaseStationReader.Terminal
{
    public static class Program
    {
        private static char[] _separators = [' ', '.'];

        private static TrackerCommandLineParser _parser = new(new HelpTabulator());
        private static ITrackerTableManager _tableManager = null;
        private static ITrackerLogger _logger = null;
        private static ITrackerWrapper _wrapper = null;
        private static TrackerApplicationSettings _settings = null;
        private static DateTime _lastUpdate = DateTime.Now;

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
                var reader = new TrackingProfileReaderWriter();
                _settings = new TrackerSettingsBuilder().BuildSettings(_parser, reader, "appsettings.json");

                // Configure the log file
                _logger = new FileLogger();
                _logger.Initialise(_settings!.LogFile, _settings.MinimumLogLevel);

                // Get the version number and application title
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
                var title = $"Aircraft Tracker v{info.FileVersion}: {_settings?.Host}:{_settings?.Port}";

                // Log the startup messages
                _logger.LogMessage(Severity.Info, new string('=', 80));
                _logger.LogMessage(Severity.Info, title);

                // Make sure the latest migrations have been applied - this ensures the DB is created and in the
                // correct state if it's absent or stale on startup
                var context = new BaseStationReaderDbContextFactory().CreateDbContext([]);
                context.Database.Migrate();
                _logger.LogMessage(Severity.Debug, "Latest database migrations have been applied");

                // Extract API lookup filtering properties from the command line arguments
                var departureAirports = GetAirportCodeList(CommandLineOptionType.Departure);
                var arrivalAirports = GetAirportCodeList(CommandLineOptionType.Arrival);

                // Initialise the tracker wrapper
                var serviceType = ApiWrapperBuilder.GetServiceTypeFromString(_settings.LiveApi);
                _wrapper = new TrackerWrapper(_logger, _settings, departureAirports, arrivalAirports, serviceType);
                await _wrapper.InitialiseAsync();
                _wrapper.AircraftAdded += OnAircraftAdded;
                _wrapper.AircraftUpdated += OnAircraftUpdated;
                _wrapper.AircraftRemoved += OnAircraftRemoved;

                do
                {
                    // Configure the table
                    var trackerIndexManager = new TrackerIndexManager();
                    _tableManager = new TrackerTableManager(trackerIndexManager, _settings!.Columns, _settings!.MaximumRows);
                    _tableManager.CreateTable(title);

                    // Construct the live view
                    await AnsiConsole.Live(_tableManager.Table!)
                        .AutoClear(true)
                        .Overflow(VerticalOverflow.Ellipsis)
                        .Cropping(VerticalOverflowCropping.Bottom)
                        .StartAsync(async ctx =>
                        {
                            await ShowTrackingTable(ctx);
                        });
                }
                while (_settings!.RestartOnTimeout);
            }
        }

        /// <summary>
        /// Display and continuously update the tracking table
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private static async Task ShowTrackingTable(LiveDisplayContext ctx)
        {
            // Reset the elapsed time since the last update
            int elapsed = 0;
            _lastUpdate = DateTime.Now;

            // Start the wrapper and continuously update the table
            _wrapper!.Start();
            while (elapsed <= _settings!.ApplicationTimeout)
            {
                // Refresh and wait for a while
                ctx.Refresh();
                await Task.Delay(100);

                // Check we've not exceeded the application timeout
                elapsed = (int)(DateTime.Now - _lastUpdate).TotalMilliseconds;
            }

            // Stop the wrapper
            _wrapper.Stop();
        }

        /// <summary>
        /// Handle the event raised when a new aircraft is detected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnAircraftAdded(object sender, AircraftNotificationEventArgs e)
        {
            // Update the timestamp used to implement the application timeout
            _lastUpdate = DateTime.Now;

            // Add the aircraft to the bottom of the table
            var rowNumber = _tableManager!.AddAircraft(e.Aircraft);
            if (rowNumber != -1)
            {
                _logger!.LogMessage(Severity.Info, $"Added new aircraft {e.Aircraft.Address} at row {rowNumber}");
            }
        }

        /// <summary>
        /// Handle the event raised when an existing aircraft is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnAircraftUpdated(object sender, AircraftNotificationEventArgs e)
        {
            // Update the timestamp used to implement the application timeout
            _lastUpdate = DateTime.Now;

            // Update the row
            var rowNumber = _tableManager!.UpdateAircraft(e.Aircraft);
            _logger!.LogMessage(Severity.Debug, $"Updated aircraft {e.Aircraft.Address} at row {rowNumber}");
        }

        /// <summary>
        /// Handle the event raised when an existing aircraft is removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnAircraftRemoved(object sender, AircraftNotificationEventArgs e)
        {
            // Update the timestamp used to implement the application timeout
            _lastUpdate = DateTime.Now;

            // Remove the aircraft from the index
            var rowNumber = _tableManager!.RemoveAircraft(e.Aircraft);
            _logger!.LogMessage(Severity.Info, $"Removed aircraft {e.Aircraft.Address} at row {rowNumber}");
        }

        /// <summary>
        /// Extract a list of airport ICAO/IATA codes from a comma-separated string
        /// </summary>
        /// <param name="type"></param>
        /// <param name="airportCodeList"></param>
        /// <returns></returns>
        public static IEnumerable<string> GetAirportCodeList(CommandLineOptionType option)
        {
            IEnumerable<string> airportCodes = null;

            // Check the option is specified
            if (_parser.IsPresent(option))
            {
                // Extract the comma-separated string from the command line options
                var airportCodeList = _parser.GetValues(option)[0];
                if (!string.IsNullOrEmpty(airportCodeList))
                {
                    // Log the list and split it list into an array of airport codes
                    _logger.LogMessage(Severity.Info, $"{option} airport code filters: {airportCodeList}");
                    airportCodes = airportCodeList.Split(_separators, StringSplitOptions.RemoveEmptyEntries);
                }
            }

            return airportCodes;
        }
    }
}