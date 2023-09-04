using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Logic.Configuration;
using BaseStationReader.Logic.Database;
using BaseStationReader.Logic.Logging;
using BaseStationReader.Logic.Messages;
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
        private static IQueuedWriter? _writer = null;
        private static ITrackerLogger? _logger = null;
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

            // Log the startup messages, including the settings
            _logger.LogMessage(Severity.Info, new string('=', 80));
            _logger.LogMessage(Severity.Info, title);
            _logger.LogMessage(Severity.Debug, $"Host = {_settings?.Host}");
            _logger.LogMessage(Severity.Debug, $"Port = {_settings?.Port}");
            _logger.LogMessage(Severity.Debug, $"SocketReadTimeout = {_settings?.SocketReadTimeout}");
            _logger.LogMessage(Severity.Debug, $"ApplicationTimeout = {_settings?.ApplicationTimeout}");
            _logger.LogMessage(Severity.Debug, $"TimeToRecent = {_settings?.TimeToRecent}");
            _logger.LogMessage(Severity.Debug, $"TimeToStale = {_settings?.TimeToStale}");
            _logger.LogMessage(Severity.Debug, $"TimeToRemoval = {_settings?.TimeToRemoval}");
            _logger.LogMessage(Severity.Debug, $"TimeToLock = {_settings?.TimeToLock}");
            _logger.LogMessage(Severity.Debug, $"LogFile = {_settings?.LogFile}");
            _logger.LogMessage(Severity.Debug, $"EnableSqlWriter = {_settings?.EnableSqlWriter}");
            _logger.LogMessage(Severity.Debug, $"WriterInterval = {_settings?.WriterInterval}");
            _logger.LogMessage(Severity.Debug, $"WriterBatchSize = {_settings?.WriterBatchSize}");

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
            // Set up the message reader and parser and the aircraft tracker
            var reader = new MessageReader(_logger!, _settings!.Host, _settings.Port, _settings.SocketReadTimeout);
            var parsers = new Dictionary<MessageType, IMessageParser>
            {
                { MessageType.MSG, new MsgMessageParser() }
            };

            // Set up the aircraft tracker
            var trackerTimer = new TrackerTimer(_settings.TimeToRecent / 10.0);
            var tracker = new AircraftTracker(reader,
                parsers,
                _logger!,
                trackerTimer,
                _settings.TimeToRecent,
                _settings.TimeToStale,
                _settings.TimeToRemoval);

            // Wire up the aircraft tracking events
            tracker.AircraftAdded += OnAircraftAdded;
            tracker.AircraftUpdated += OnAircraftUpdated;
            tracker.AircraftRemoved += OnAircraftRemoved;

            // Set up the queued database writer
            if (_settings.EnableSqlWriter)
            {
                BaseStationReaderDbContext context = new BaseStationReaderDbContextFactory().CreateDbContext(Array.Empty<string>());
                var aircraftWriter = new AircraftWriter(context);
                var positionWriter = new PositionWriter(context);
                var aircraftLocker = new AircraftLockManager(aircraftWriter, _settings.TimeToLock);
                var writerTimer = new TrackerTimer(_settings.WriterInterval);
                _writer = new QueuedWriter(aircraftWriter, positionWriter, aircraftLocker, _logger!, writerTimer, _settings.WriterBatchSize);
                _writer.BatchWritten += OnBatchWritten;
                _writer.Start();
            }

            // Continously update the table
            int elapsed = 0;
            tracker.Start();
            while (elapsed <= _settings.ApplicationTimeout)
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

            // Push the change to the SQL writer, if enabled
            if (_settings!.EnableSqlWriter)
            {
                _logger!.LogMessage(Severity.Debug, $"Queueing aircraft {e.Aircraft.Address} for writing");
#pragma warning disable CS8602
                _writer.Push(e.Aircraft);
#pragma warning restore CS8602
                if (e.Position != null)
                {
                    _logger!.LogMessage(Severity.Debug, $"Queueing position for aircraft {e.Aircraft.Address} for writing");
                    _writer.Push(e.Position);
                }
            }

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

            // Push the change to the SQL writer, if enabled
            if (_settings!.EnableSqlWriter)
            {
                _logger!.LogMessage(Severity.Debug, $"Queueing aircraft {e.Aircraft.Address} for writing");
#pragma warning disable CS8602
                _writer.Push(e.Aircraft);
#pragma warning restore CS8602
                if (e.Position != null)
                {
                    _logger!.LogMessage(Severity.Debug, $"Queueing position for aircraft {e.Aircraft.Address} for writing");
                    _writer.Push(e.Position);
                }
            }

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

        /// <summary>
        /// Handle the event raised when a batch of aircraft updates are written to the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnBatchWritten(object? sender, BatchWrittenEventArgs e)
        {
            _logger!.LogMessage(Severity.Info, $"Aircraft batch written to the database. Queue size {e.InitialQueueSize} -> {e.FinalQueueSize} in {e.Duration} ms");
        }
    }
}