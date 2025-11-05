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
using BaseStationReader.TrackerHub.Logic;
using BaseStationReader.BusinessLogic.TrackerHub.Logic;
using Microsoft.AspNetCore.StaticFiles;
using BaseStationReader.Interfaces.Hub;
using System.Runtime.Loader;

namespace BaseStationReader.TrackerHub
{
    public static class Program
    {
        private static char[] _separators = [' ', '.'];

        private static TrackerCommandLineParser _parser = new(new HelpTabulator());
        private static ITrackerLogger _logger = null;
        private static ITrackerIndexManager _trackerIndexManager = null;
        private static ITrackerController _controller = null;
        private static IEventBridge _bridge = null;
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

                // Locate the static files root
                var contentRootPath = Path.Exists("wwwroot") ? Directory.GetCurrentDirectory() : AppContext.BaseDirectory;

                // Create a web application builder
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions
                {
                    Args = args,
                    ContentRootPath = contentRootPath,
                    WebRootPath = Path.Combine(contentRootPath, "wwwroot")
                });

                // Bind Kestrel options from the applicatiokn settings file
                builder.WebHost.ConfigureKestrel(options =>
                {
                    builder.Configuration.GetSection("Kestrel").Bind(options);
                });

                // Register SignalR
                builder.Services.AddSignalR().AddMessagePackProtocol();
                builder.Services.AddResponseCompression(o => o.EnableForHttps = true);

                // Register the aircraft state and event bridge
                builder.Services.AddSingleton<IEventBridge, EventBridge>();
                builder.Services.AddSingleton<ITrackerController>(_controller);
                builder.Services.AddSingleton<ITrackerLogger>(_logger);
                builder.Services.AddHostedService(sp => (EventBridge)sp.GetRequiredService<IEventBridge>());

                // Set the CORS policy
                builder.Services.AddCors(o => o.AddPolicy("development", p => p
                    .AllowAnyOrigin()
                    .AllowAnyHeader()
                    .AllowAnyMethod()));

                // Build the web application
                var app = builder.Build();
                app.UseResponseCompression();
                app.UseCors("development");
                app.UseDefaultFiles();

                // Serve static files from wwwroot, ensuring the ".map" files are recognised and served as JSON
                var provider = new FileExtensionContentTypeProvider();
                provider.Mappings[".map"] = "application/json";
                app.UseStaticFiles(new StaticFileOptions {
                    ContentTypeProvider = provider
                });

                // Map the endpoint
                app.MapHub<AircraftHub>("/hubs/aircraft");

                // Configure cancellation
                using var source = new CancellationTokenSource();

                // Cancel on Ctrl-C (SIGINT)
                Console.CancelKeyPress += (s, e) =>
                {
                    // Shut down gracefully rather than immediately killing the process
                    e.Cancel = true;
                    if (!source.IsCancellationRequested) source.Cancel();
                };

                // Cancel on SIGTERM / docker stop
                AssemblyLoadContext.Default.Unloading += _ =>
                {
                    if (!source.IsCancellationRequested) source.Cancel();
                };

                // Cancel on app lifetime stop signals (e.g., triggered by Kestrel or hosting)
                app.Lifetime.ApplicationStopping.Register(() =>
                {
                    if (!source.IsCancellationRequested) source.Cancel();
                });

                // Treat Ctrl-C as a cancel signal, not a keypress
                Console.TreatControlCAsInput = false;

                // Get the event bridge so the event handler can publish to it
                _bridge = app.Services.GetRequiredService<IEventBridge>();

                // Start the web application and the tracker controller tasks on the same token
                var webAppTask = app.RunAsync(source.Token);
                var trackerControllerTask = RunMainAsync(source.Token);

                // Wait for one of the tasks to complete
                await Task.WhenAny(webAppTask, trackerControllerTask);

                // If one side ends due to e.g. error, ESC, timeout, cancel the other and wait a moment to flush
                source.Cancel();
                try
                {
                    await Task.WhenAll(webAppTask, trackerControllerTask);
                }
                catch (OperationCanceledException)
                {
                    // Expected on cancellation
                }

                // Process all pending requests in the queued writer queue
                if (_settings.EnableSqlWriter)
                {
                    Console.WriteLine($"Processing {_controller.QueueSize} pending database updates and API requests ...");
                    await _controller.FlushQueueAsync();
                }
            }
        }
        
        /// <summary>
        /// Run the main event loop for the cancellation keypress and tracker controller
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        static async Task RunMainAsync(CancellationToken token)
        {
            // Kick off a background key listener
            var keyListenerTask = ListenForCancellationKeypressAsync(token);

            bool restart;
            do
            {
                // Create a linked token source for the tracker loop
                using (var trackerLoopTokenSource = CancellationTokenSource.CreateLinkedTokenSource(token))
                {
                    // Run the tracker loop - this will return 
                    restart = await RunTrackerEventLoopAsync(trackerLoopTokenSource.Token).ConfigureAwait(false)
                            && _settings.RestartOnTimeout
                            && !token.IsCancellationRequested;
                }
            }
            while (restart);

            // Wait for the key listener (ignore cancellation)
            try
            {
                await keyListenerTask;
            }
            catch (OperationCanceledException)
            {
                // Expected on cancellation
            }
        }

        /// <summary>
        /// Display and continuously update the tracking table
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        private static async Task<bool> RunTrackerEventLoopAsync(CancellationToken token)
        {
            // Reset the elapsed time since the last update
            _lastUpdate = DateTime.Now;

            // Wire up the aircraft notificarion event handlers
            _controller.AircraftEvent += OnAircraftEvent;

            // Create a cancellation token and start the controller task
            var controllerTask = _controller.StartAsync(token);

            // Define the interval at which the display will refresh
            var interval = TimeSpan.FromMilliseconds(100);

            try
            {
                while (!token.IsCancellationRequested)
                {
                    // If we've exceeded the application timeout since the last update, break out to the caller
                    var elapsed = (DateTime.Now - _lastUpdate).TotalMilliseconds;
                    if ((_settings.ApplicationTimeout > 0) && (elapsed > _settings.ApplicationTimeout))
                    {
                        throw new OperationCanceledException(token);
                    }

                    var delayTask = Task.Delay(interval, token);
                    var winner = await Task.WhenAny(controllerTask, delayTask).ConfigureAwait(false);

                    if (winner == controllerTask)
                    {
                        // This propagates completion/exception/cancellation
                        await controllerTask.ConfigureAwait(false);
                        break;
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

            return !token.IsCancellationRequested && _settings.RestartOnTimeout;
        }
        
        /// <summary>
        /// Background key listener that is safe on the console and a no-op if there is no console
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="OperationCanceledException"></exception>
        private static async Task ListenForCancellationKeypressAsync(CancellationToken token)
        {
            if (!Console.IsInputRedirected)
            {
                while (!token.IsCancellationRequested)
                {
                    // ReadKey is blocking; run it on a thread pool thread
                    var keyInfo = await Task.Run(() => Console.ReadKey(intercept: true), token);
                    if (keyInfo.Key == ConsoleKey.Escape)
                    {
                        throw new OperationCanceledException(token);
                    }
                }
            }
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

            // Log and signal the event
            _logger.LogMessage(Severity.Info, $"Received {e.NotificationType} event for aircraft {e.Aircraft.Address}");
            _ = Task.Run(() => _bridge.PublishAsync(e));

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