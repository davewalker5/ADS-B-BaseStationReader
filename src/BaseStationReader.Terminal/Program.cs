using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Tracking;
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
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.BusinessLogic.Messages;
using System.Collections.Concurrent;

namespace BaseStationReader.Terminal
{
    public static class Program
    {
        private static TrackerCommandLineParser _parser = new(new HelpTabulator(), includeFlushOnStop: true);
        private static ITrackerTableManager _tableManager = null;
        private static ITrackerLogger _logger = null;
        private static ITrackerController _controller = null;
        private static TrackerApplicationSettings _settings = null;
        private static DateTime _lastUpdate = DateTime.Now;
        private static readonly ConcurrentDictionary<string, AircraftNotificationEventArgs> _pendingAircraftEvents =
            new(StringComparer.OrdinalIgnoreCase);

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
                _logger.Initialise(_settings.LogFile, _settings.MinimumLogLevel, _settings.VerboseLogging);

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

                // Initialise the tracker wrapper
                var tcpClient = new TrackerTcpClient();
                var densityStateManager = new PositionDensitySnapshotStateManager(new PositionDensitySnapshotMerger());
                var densityOrchestrator = new PositionDensitySnapshotOrchestrator(
                    new PositionDensityAggregator(),
                    densityStateManager);
                _controller = new TrackerController(
                    _logger,
                    context,
                    tcpClient,
                    _settings,
                    densityOrchestrator: densityOrchestrator,
                    densitySnapshotMapper: new PositionDensitySnapshotMapper());

                var cancelled = false;
                do
                {
                    // Configure the table
                    var trackerIndexManager = new TrackerIndexManager();
                    _tableManager = new TrackerTableManager(trackerIndexManager, _settings.Columns, _settings.MaximumRows);
                    _tableManager.CreateTable(title);

                    // Construct the live view
                    await AnsiConsole.Live(_tableManager.Table!)
                        .AutoClear(true)
                        .Overflow(VerticalOverflow.Ellipsis)
                        .Cropping(VerticalOverflowCropping.Bottom)
                        .StartAsync(async ctx =>
                        {
                            cancelled = await ShowTrackingTable(ctx);
                        });
                }
                while (_settings.RestartOnTimeout && !cancelled);

                // The controller has already applied the configured stop-flush policy. Report any
                // durable entries left for later replay without overriding FlushOnStop here.
                if (_settings.EnableSqlWriter && _controller.QueueSize > 0)
                {
                    Console.WriteLine($"Retaining {_controller.QueueSize} pending database updates for later replay.");
                }
            }
        }

        /// <summary>
        /// Display and continuously update the tracking table
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private static async Task<bool> ShowTrackingTable(LiveDisplayContext ctx)
        {
            bool cancelled = false;

            // Reset the elapsed time since the last update
            _lastUpdate = DateTime.Now;
            _pendingAircraftEvents.Clear();

            // Wire up the aircraft notificarion event handlers
            _controller.AircraftEvent += OnAircraftEvent;

            // Create a cancellation token and start the controller task
            using var source = new CancellationTokenSource();
            var controllerTask = _controller.StartAsync(source.Token);

            // Define the interval at which the display will refresh
            var interval = TimeSpan.FromMilliseconds(Math.Max(100, _settings.RefreshInterval));

            try
            {
                while (!cancelled && !source.Token.IsCancellationRequested)
                {
                    // If we've exceeded the application timeout since the last update, request cancellation
                    var elapsed = (DateTime.Now - _lastUpdate).TotalMilliseconds;
                    if ((_settings.ApplicationTimeout > 0) && (elapsed > _settings.ApplicationTimeout))
                    {
                        source.Cancel();
                    }

                    var delayTask = Task.Delay(interval, source.Token);
                    var winner = await Task.WhenAny(controllerTask, delayTask).ConfigureAwait(false);

                    if (winner == controllerTask)
                    {
                        // This propagates completion/exception/cancellation
                        await controllerTask.ConfigureAwait(false); 
                        break;
                    }

                    // Refresh the display and check for the cancellation keypress 
                    cancelled = RefreshTable(ctx);
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the token is cancelled
            }
            finally
            {
                // Close the producer boundary before cancellation, then wait until all session-owned
                // components have stopped and the configured FlushOnStop policy has been applied.
                _controller.RequestStop();
                source.Cancel();

                try
                {
                    await controllerTask.ConfigureAwait(false);
                }
                catch (OperationCanceledException) when (source.IsCancellationRequested)
                {
                    // Expected when tracking is stopped by Escape or the inactivity timeout.
                }

                // Detach from the tracker controller
                _controller.AircraftEvent -= OnAircraftEvent;
            }

            return cancelled;
        }

        /// <summary>
        /// Refresh the display and check for the cancellation keypress
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private static bool RefreshTable(LiveDisplayContext ctx)
        {
            bool cancelled = false;

            // See if there's a keypress available
            if (Console.KeyAvailable)
            {
                // There is, so read it
                var key = Console.ReadKey(true);
                if (key.Key == ConsoleKey.Escape)
                {
                    // It's the ESC key so set the cancelled flag and break out
                    cancelled = true;
                }
            }

            // Apply only the newest pending state for each aircraft before rendering. This bounds
            // display work without allowing the terminal to fall behind the message feed.
            foreach (var entry in _pendingAircraftEvents.ToArray())
            {
                if (!_pendingAircraftEvents.TryRemove(entry.Key, out var aircraftEvent))
                {
                    continue;
                }

                if (aircraftEvent.NotificationType == AircraftNotificationType.Removed)
                {
                    _tableManager.RemoveAircraft(aircraftEvent.Aircraft);
                }
                else
                {
                    _tableManager.AddOrUpdateAircraft(aircraftEvent.Aircraft);
                }
            }

            // Refresh
            ctx.Refresh();

            return cancelled;
        }

        /// <summary>
        /// Handle an aircraft event
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnAircraftEvent(object sender, AircraftNotificationEventArgs e)
        {
            // Update the timestamp used to implement the application timeout
            _lastUpdate = DateTime.Now;

            // Coalesce a burst to the latest state for this aircraft. The display loop drains this
            // collection at its own refresh interval.
            _pendingAircraftEvents[e.Aircraft.Address] = e;
        }

    }
}
