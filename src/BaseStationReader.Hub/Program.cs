using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.BusinessLogic.Tracking;
using Microsoft.EntityFrameworkCore;
using System.Diagnostics;
using System.Reflection;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Api.Wrapper;
using BaseStationReader.Api;
using BaseStationReader.BusinessLogic.Messages;
using BaseStationReader.Hub.Logic;

namespace BaseStationReader.Terminal
{
    public static class Program
    {
        private static char[] _separators = [' ', '.'];

        private static TrackerCommandLineParser _parser = new(new HelpTabulator());
        private static ITrackerLogger _logger = null;
        private static ITrackerIndexManager _trackerIndexManager = null;
        private static TrackerController _controller = null;
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
                _logger.Initialise(_settings.LogFile, _settings.MinimumLogLevel, _settings.VerboseLogging);

                // Get the version number and application title
                Assembly assembly = Assembly.GetExecutingAssembly();
                FileVersionInfo info = FileVersionInfo.GetVersionInfo(assembly.Location);
                var title = $"Aircraft Tracker Hub v{info.FileVersion}: {_settings?.Host}:{_settings?.Port}";

                // Log the startup messages
                _logger.LogMessage(Severity.Info, new string('=', 80));
                _logger.LogMessage(Severity.Info, title);

                // Show the startup messages
                Console.WriteLine(new string('=', 80));
                Console.WriteLine(title);
                Console.WriteLine($"Output will be logged to {_settings.LogFile}");
                Console.WriteLine("Press ESC to stop the hub");

                // Make sure the latest migrations have been applied - this ensures the DB is created and in the
                // correct state if it's absent or stale on startup
                var context = new BaseStationReaderDbContextFactory().CreateDbContext([]);
                context.Database.Migrate();
                _logger.LogMessage(Severity.Debug, "Latest database migrations have been applied");

                // Extract API lookup filtering properties from the command line arguments
                var departureAirports = GetAirportCodeList(CommandLineOptionType.Departure);
                var arrivalAirports = GetAirportCodeList(CommandLineOptionType.Arrival);

                // Initialise the tracker wrapper
                var apiFactory = new ExternalApiFactory();
                var httpClient = TrackerHttpClient.Instance;
                var tcpClient = new TrackerTcpClient();
                _controller = new TrackerController(_logger, context, apiFactory, httpClient, tcpClient, _settings, departureAirports, arrivalAirports);

                var cancelled = false;
                do
                {
                    // Configure the table
                    _trackerIndexManager = new TrackerIndexManager();
                    cancelled = await RunEventLoopAsync();
                }
                while (_settings.RestartOnTimeout && !cancelled);

                // Process all pending requests in the queued writer queue
                if (_settings.EnableSqlWriter)
                {
                    Console.WriteLine($"Processing {_controller.QueueSize} pending database updates and API requests ...");
                    await _controller.FlushQueueAsync();
                }
            }
        }
        
        /// <summary>
        /// Display and continuously update the tracking table
        /// </summary>
        /// <param name="ctx"></param>
        /// <returns></returns>
        private static async Task<bool> RunEventLoopAsync()
        {
            bool cancelled = false;

            // Reset the elapsed time since the last update
            _lastUpdate = DateTime.Now;

            // Wire up the aircraft notificarion event handlers
            _controller.AircraftEvent += OnAircraftEvent;

            // Create a cancellation token and start the controller task
            using var source = new CancellationTokenSource();
            var controllerTask = _controller.StartAsync(source.Token);

            // Define the interval at which the display will refresh
            var interval = TimeSpan.FromMilliseconds(100);

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

                    // See if there's a keypress available
                    if (!cancelled && Console.KeyAvailable)
                    {
                        // There is, so read it
                        var key = Console.ReadKey(true);
                        if (key.Key == ConsoleKey.Escape)
                        {
                            // It's the ESC key so set the cancelled flag and break out
                            cancelled = true;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected when the token is cancelled
            }
            finally
            {
                // Detach from the tracker controller
                _controller.AircraftEvent -= OnAircraftEvent;
            }

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

            // TODO : Log and signal the event
            _logger.LogMessage(Severity.Info, $"Received {e.NotificationType} event for aircraft {e.Aircraft.Address}");

            if (e.NotificationType == AircraftNotificationType.Removed)
            {
                // Remove the aircraft details from the cache
                _ = _trackerIndexManager.RemoveAircraft(e.Aircraft.Address);
            }
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