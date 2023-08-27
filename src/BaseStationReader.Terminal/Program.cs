using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic;
using Serilog;
using Spectre.Console;
using System.Diagnostics;
using System.Reflection;

namespace BaseStationReader.Terminal
{
    public class Program
    {
        private readonly static Table _table = new Table().Expand().BorderColor(Spectre.Console.Color.Grey);
        private readonly static Dictionary<string, int> _rowIndex = new();
        private static IQueuedWriter? _writer = null;

        public static async Task Main(string[] args)
        {
            // Read the application config
            ApplicationSettings? settings = new ConfigReader().Read("appsettings.json");

            // Configure the log file
#pragma warning disable CS8602
            Log.Logger = new LoggerConfiguration()
                .WriteTo
                .File(
                    settings.LogFile,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true)
                .CreateLogger();
#pragma warning restore CS8602

            // Get the assembly information, construct the table title and log the start-up message
            Assembly assembly = Assembly.GetExecutingAssembly();
            FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
            var title = $"Aircraft Tracker v{info.FileVersion}: {settings.Host}:{settings.Port}";
            _table.Title(title);
            Log.Information(new string('=', 80));
            Log.Information(title);

            // Configure the table columns
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
                    await ShowTrackingTable(ctx, settings);
                });
        }

        /// <summary>
        /// Display and continuously update the tracking table
        /// </summary>
        /// <param name="ctx"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        private static async Task ShowTrackingTable(LiveDisplayContext ctx, ApplicationSettings settings)
        {
            // Set up the message reader and parser and the aircraft tracker
            var reader = new MessageReader(settings.Host, settings.Port);
            var parsers = new Dictionary<MessageType, IMessageParser>
            {
                { MessageType.MSG, new MsgMessageParser() }
            };

            // Set up the aircraft tracker
            var trackerTimer = new TrackerTimer(settings.TimeToRecent / 10.0);
            var tracker = new AircraftTracker(reader, parsers, trackerTimer, settings.TimeToRecent, settings.TimeToStale, settings.TimeToRemoval);

            // Set up the queued database writer
            BaseStationReaderDbContext context = new BaseStationReaderDbContextFactory().CreateDbContext(Array.Empty<string>());
            var manager = new AircraftManager(context);
            var writerTimer = new TrackerTimer(settings.WriterInterval);
            _writer = new QueuedWriter(manager, writerTimer, settings.WriterBatchSize);

            // Wire up the aircraft tracking events
            tracker.AircraftAdded += OnAircraftAdded;
            tracker.AircraftUpdated += OnAircraftUpdated;
            tracker.AircraftRemoved += OnAircraftRemoved;
            _writer.BatchWritten += OnBatchWritten;

            // Continously update the table
            _writer.Start();
            tracker.Start();
            while (true)
            {
                // Refresh and wait for a while
                ctx.Refresh();
                await Task.Delay(100);
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
#pragma warning disable CS8602
                _writer.Push(e.Aircraft);
#pragma warning restore CS8602
                var rowIndex = _table.Rows.Count;
                var rowData = GetAircraftRowData(e.Aircraft);
                _table.AddRow(rowData);
                _rowIndex.Add(e.Aircraft.Address, rowIndex);
                Log.Information($"Added new aircraft {e.Aircraft.Address} at row {rowIndex}");
            }
        }

        /// <summary>
        /// Handle the event raised when an existing aircraft is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnAircraftUpdated(object? sender, AircraftNotificationEventArgs e)
        {
#pragma warning disable CS8602
            _writer.Push(e.Aircraft);
#pragma warning restore CS8602
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
                // Lock the aircraft record - if we see it again, a new record will be created
                e.Aircraft.Locked = true;
#pragma warning disable CS8602
                _writer.Push(e.Aircraft);
#pragma warning restore CS8602

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
                Log.Information($"Removed aircraft {e.Aircraft.Address} at row {row}");
            }
        }

        /// <summary>
        /// Handle the event raised when a batch of aircraft updates are written to the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private static void OnBatchWritten(object? sender, BatchWrittenEventArgs e)
        {
            Log.Information($"Aircraft batch written to the database. Queue size {e.InitialQueueSize} -> {e.FinalQueueSize} in {e.Duration} ms");
        }
    }
}