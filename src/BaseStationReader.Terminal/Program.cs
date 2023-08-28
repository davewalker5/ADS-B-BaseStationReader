using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic;
using Serilog;
using Spectre.Console;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;

namespace BaseStationReader.Terminal
{
    [ExcludeFromCodeCoverage]
    public static class Program
    {
        private readonly static Table _table = new Table().Expand().BorderColor(Spectre.Console.Color.Grey);
        private readonly static Dictionary<string, int> _rowIndex = new();
        private static IQueuedWriter? _writer = null;
        private static ITrackerLogger? _logger = null;
        private static ApplicationSettings? _settings = null;
        private static DateTime _lastUpdate = DateTime.Now;

        public static async Task Main(string[] args)
        {
            // Read the application config file
            _settings = BuildSettings(args);

            // Configure the log file
            _logger = new FileLogger();
            _logger.Initialise(_settings!.LogFile);

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
            _logger.LogMessage(Severity.Debug, $"LogFile = {_settings?.LogFile}");
            _logger.LogMessage(Severity.Debug, $"EnableSqlWriter = {_settings?.EnableSqlWriter}");
            _logger.LogMessage(Severity.Debug, $"WriterInterval = {_settings?.WriterInterval}");
            _logger.LogMessage(Severity.Debug, $"WriterBatchSize = {_settings?.WriterBatchSize}");

            // Configure the table title and columns
            _table.Title(title);
            _table.AddColumn("[yellow]ID[/]");
            _table.AddColumn("[yellow]Callsign[/]");
            _table.AddColumn("[yellow]Squawk[/]");
            _table.AddColumn("[yellow]Altitude[/]");
            _table.AddColumn("[yellow]Speed[/]");
            _table.AddColumn("[yellow]Track[/]");
            _table.AddColumn("[yellow]Latitude[/]");
            _table.AddColumn("[yellow]Longitude[/]");
            _table.AddColumn("[yellow]First Seen[/]");
            _table.AddColumn("[yellow]Last Seen[/]");

            // Construct the live view
            await AnsiConsole.Live(_table)
                .AutoClear(false)
                .Overflow(VerticalOverflow.Ellipsis)
                .Cropping(VerticalOverflowCropping.Bottom)
                .StartAsync(async ctx =>
                {
                    await ShowTrackingTable(ctx);
                });
        }

        /// <summary>
        /// Construct the application settings from the configuration file and any command line arguments
        /// </summary>
        /// <param name="args"></param>
        /// <returns></returns>
        private static ApplicationSettings? BuildSettings(IEnumerable<string> args)
        {
            // Read the config file to provide default settings
            var settings = ConfigReader.Read("appsettings.json");

            // Parse the command line
            var parser = new CommandLineParser();
            parser.Add(CommandLineOptionType.Host, false, "--host", "-h", "Host to connect to for data stream", 1, 1);
            parser.Add(CommandLineOptionType.Port, false, "--port", "-p", "Port to connect to for data stream", 1, 1);
            parser.Add(CommandLineOptionType.SocketReadTimeout, false, "--read-timeout", "-t", "Timeout (ms) for socket read operations", 1, 1);
            parser.Add(CommandLineOptionType.ApplicationTimeout, false, "--app-timeout", "-a", "Timeout (ms) after which the application will quit of no messages are recieved", 1, 1);
            parser.Add(CommandLineOptionType.TimeToRecent, false, "--recent", "-r", "Time (ms) to 'recent' staleness", 1, 1);
            parser.Add(CommandLineOptionType.TimeToStale, false, "--stale", "-s", "Time (ms) to 'stale' staleness", 1, 1);
            parser.Add(CommandLineOptionType.TimeToRemoval, false, "--remove", "-x", "Time (ms) removal of stale records", 1, 1);
            parser.Add(CommandLineOptionType.LogFile, false, "--log-file", "-l", "Log file path and name", 1, 1);
            parser.Add(CommandLineOptionType.EnableSqlWriter, false, "--enable-sql-writer", "-w", "Log file path and name", 1, 1);
            parser.Add(CommandLineOptionType.WriterInterval, false, "--writer-interval", "-i", "SQL write interval (ms)", 1, 1);
            parser.Add(CommandLineOptionType.WriterBatchSize, false, "--writer-batch-size", "-b", "SQL write batch size", 1, 1);
            parser.Parse(args);

            // Apply the command line values over the defaults
            var values = parser.GetValues(CommandLineOptionType.Host);
            if (values != null) settings!.Host = values[0];

            values = parser.GetValues(CommandLineOptionType.Port);
            if (values != null) settings!.Port = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.SocketReadTimeout);
            if (values != null) settings!.SocketReadTimeout = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ApplicationTimeout);
            if (values != null) settings!.ApplicationTimeout = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToRecent);
            if (values != null) settings!.TimeToRecent = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToStale);
            if (values != null) settings!.TimeToStale = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToRemoval);
            if (values != null) settings!.TimeToRemoval = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.LogFile);
            if (values != null) settings!.LogFile = values[0];

            values = parser.GetValues(CommandLineOptionType.EnableSqlWriter);
            if (values != null) settings!.EnableSqlWriter = bool.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.WriterInterval);
            if (values != null) settings!.WriterInterval = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.WriterBatchSize);
            if (values != null) settings!.WriterBatchSize = int.Parse(values[0]);

            return settings;
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
            var tracker = new AircraftTracker(reader, parsers, _logger!, trackerTimer, _settings.TimeToRecent, _settings.TimeToStale, _settings.TimeToRemoval);

            // Wire up the aircraft tracking events
            tracker.AircraftAdded += OnAircraftAdded;
            tracker.AircraftUpdated += OnAircraftUpdated;
            tracker.AircraftRemoved += OnAircraftRemoved;

            // Set up the queued database writer
            if (_settings.EnableSqlWriter)
            {
                BaseStationReaderDbContext context = new BaseStationReaderDbContextFactory().CreateDbContext(Array.Empty<string>());
                var manager = new AircraftManager(context);
                var writerTimer = new TrackerTimer(_settings.WriterInterval);
                _writer = new QueuedWriter(manager, _logger!, writerTimer, _settings.WriterBatchSize);
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
        /// Construct and return a row of data for the specified aircraft
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private static string[] GetAircraftRowData(Aircraft aircraft)
        {
            // Use the aircraft's staleness to set the row colour
            var startColour = "";
            var endColour = "";
            if (aircraft.Staleness == Staleness.Stale)
            {
                startColour = "[red]";
                endColour = "[/]";
            }
            else if (aircraft.Staleness == Staleness.Recent)
            {
                startColour = "[yellow]";
                endColour = "[/]";
            }

            // Construct and return the markup
            return new string[]
            {
                $"{startColour}{aircraft.Address}{endColour}",
                $"{startColour}{aircraft.Callsign ?? ""}{endColour}",
                $"{startColour}{aircraft.Squawk ?? ""}{endColour}",
                $"{startColour}{aircraft.Altitude.ToString() ?? ""}{endColour}",
                $"{startColour}{aircraft.GroundSpeed.ToString() ?? ""}{endColour}",
                $"{startColour}{aircraft.Track.ToString() ?? ""}{endColour}",
                $"{startColour}{aircraft.Latitude.ToString() ?? ""}{endColour}",
                $"{startColour}{aircraft.Longitude.ToString() ?? ""}{endColour}",
                $"{startColour}{aircraft.FirstSeen.ToString("HH:mm:ss.fff") ?? ""}{endColour}",
                $"{startColour}{aircraft.LastSeen.ToString("HH:mm:ss.fff") ?? ""}{endColour}",
            };
        }

        /// <summary>
        /// Handle the event raised when a new aircraft is detected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnAircraftAdded(object? sender, AircraftNotificationEventArgs e)
        {
            lock (_rowIndex)
            {
                // Update the timestamp used to implement the application timeout
                _lastUpdate = DateTime.Now;

                // Push the change to the SQL writer, if enabled
                if (_settings!.EnableSqlWriter)
                {
#pragma warning disable CS8602
                    _writer.Push(e.Aircraft);
#pragma warning restore CS8602
                }

                // Add the aircraft to the table
                var rowIndex = _table.Rows.Count;
                var rowData = GetAircraftRowData(e.Aircraft);
                _table.AddRow(rowData);
                _rowIndex.Add(e.Aircraft.Address, rowIndex);
                _logger!.LogMessage(Severity.Info, $"Added new aircraft {e.Aircraft.Address} at row {rowIndex}");
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
#pragma warning disable CS8602
                _writer.Push(e.Aircraft);
#pragma warning restore CS8602
            }

            // Update the row
            var rowIndex = _rowIndex[e.Aircraft.Address];
            var rowData = GetAircraftRowData(e.Aircraft);
            _table.RemoveRow(rowIndex);
            _table.InsertRow(rowIndex, rowData);
        }

        /// <summary>
        /// Handle the event raised when an existing aircraft is removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnAircraftRemoved(object? sender, AircraftNotificationEventArgs e)
        {
            lock (_rowIndex)
            {
                // Update the timestamp used to implement the application timeout
                _lastUpdate = DateTime.Now;

                // Lock the aircraft record - if we see it again, a new record will be created
                e.Aircraft.Locked = true;
                if (_settings!.EnableSqlWriter)
                {
#pragma warning disable CS8602
                    _writer.Push(e.Aircraft);
#pragma warning restore CS8602
                }

                // Locate the entry in the table and remove it
                var row = _rowIndex[e.Aircraft.Address];
                _table.RemoveRow(row);

                // Shuffle the index for subsequent rows
                foreach (var entry in _rowIndex)
                {
                    if (entry.Value > row)
                    {
                        _rowIndex[entry.Key] -= 1;
                    }
                }

                // Remove the record from the index
                _rowIndex.Remove(e.Aircraft.Address);
                _logger!.LogMessage(Severity.Info, $"Removed aircraft {e.Aircraft.Address} at row {row}");
            }
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