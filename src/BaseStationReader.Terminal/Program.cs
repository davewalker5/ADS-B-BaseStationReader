using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Logic.Configuration;
using BaseStationReader.Logic.Logging;
using BaseStationReader.Logic.Tracking;
using BaseStationReader.Terminal.Interfaces;
using BaseStationReader.Terminal.Logic;
using Spectre.Console;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BaseStationReader.Terminal
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private static ITrackerTableManager? _tableManager = null;
        private static ITrackerLogger? _logger = null;
        private static ITrackerWrapper? _wrapper = null;
        private static ApplicationSettings? _settings = null;
        private static DateTime _lastUpdate = DateTime.Now;

        public static async Task Main(string[] args)
        {
            // Read the application config file
            _settings = new TrackerSettingsBuilder().BuildSettings(args, "appsettings.json");

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

            // Initialise the tracker wrapper
            _wrapper = new TrackerWrapper(_logger, _settings!);
            _wrapper.Initialise();
            _wrapper.AircraftAdded += OnAircraftAdded;
            _wrapper.AircraftUpdated += OnAircraftUpdated;
            _wrapper.AircraftRemoved += OnAircraftRemoved;

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

        /// <summary>
        /// Display and continuously update the tracking table
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private static async Task ShowTrackingTable(LiveDisplayContext ctx)
        {
            // Continously update the table
            int elapsed = 0;
            _wrapper!.Start();
            while (elapsed <= _settings!.ApplicationTimeout)
            {
                // Refresh and wait for a while
                ctx.Refresh();
                await Task.Delay(100);

                // Check we've not exceeded the application timeout
#pragma warning disable S6561
                elapsed = (int)(DateTime.Now - _lastUpdate).TotalMilliseconds;
#pragma warning restore S6561
            }
        }

        /// <summary>
        /// Handle the event raised when a new aircraft is detected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnAircraftAdded(object? sender, AircraftNotificationEventArgs e)
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
        private static void OnAircraftUpdated(object? sender, AircraftNotificationEventArgs e)
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
        private static void OnAircraftRemoved(object? sender, AircraftNotificationEventArgs e)
        {
            // Update the timestamp used to implement the application timeout
            _lastUpdate = DateTime.Now;

            // Remove the aircraft from the index
            var rowNumber = _tableManager!.RemoveAircraft(e.Aircraft);
            _logger!.LogMessage(Severity.Info, $"Removed aircraft {e.Aircraft.Address} at row {rowNumber}");
        }
    }
}