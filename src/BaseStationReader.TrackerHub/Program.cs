using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Tracking;
using Microsoft.EntityFrameworkCore;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.BusinessLogic.Messages;
using BaseStationReader.TrackerHub.Logic;
using BaseStationReader.BusinessLogic.TrackerHub.Logic;
using Microsoft.AspNetCore.StaticFiles;
using BaseStationReader.Interfaces.Hub;
using System.Runtime.Loader;
using BaseStationReader.TrackerHub.Components;
using BaseStationReader.TrackerHub.Models;
using BaseStationReader.TrackerHub.Services;

namespace BaseStationReader.TrackerHub
{
    public static class Program
    {
        private static TrackerCommandLineParser _parser = new(new HelpTabulator());
        private static ITrackerLogger _logger = null;
        private static ITrackerIndexManager _trackerIndexManager = null;
        private static ITrackerController _controller = null;
        private static IEventBridge _bridge = null;
        private static TrackerApplicationSettings _settings = null;
        private static DateTime _lastUpdate = DateTime.Now;

        /// <summary>
        /// Starts the tracker, SignalR hub, and unified browser interface.
        /// </summary>
        /// <param name="args">Command-line arguments used to configure the tracker.</param>
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

                // Build the application title from the same assembly metadata displayed by the web UI.
                var title = $"Aircraft Tracker Hub v{TrackerHubVersion.Current}: {_settings?.Host}:{_settings?.Port}";

                // Log the startup messages
                _logger.LogMessage(Severity.Info, new string('=', 80));
                _logger.LogMessage(Severity.Info, title);

                // Show the startup messages
                Console.WriteLine(new string('=', 80));
                Console.WriteLine(title);
                Console.WriteLine($"Output will be logged to {_settings.LogFile}");
                Console.WriteLine("Press ESC to stop the hub");

                // Build the environment-aware ASP.NET Core configuration before creating any database context.
                var contentRootPath = Path.Exists("wwwroot") ? Directory.GetCurrentDirectory() : AppContext.BaseDirectory;
                var builder = WebApplication.CreateBuilder(new WebApplicationOptions
                {
                    Args = args,
                    ContentRootPath = contentRootPath,
                    WebRootPath = Path.Combine(contentRootPath, "wwwroot")
                });
                var defaultSettings = TrackingProfileService.Clone(_settings);

                // Restore the last UI-selected profile unless an explicit command-line profile was supplied.
                if (_parser.GetValues(CommandLineOptionType.Profile) is null)
                {
                    var profilesPath = TrackingProfileService.ResolveProfilesPath(
                        builder.Configuration["WebUi:TrackingProfilesPath"], contentRootPath);
                    var activeProfilePath = Path.Combine(profilesPath, TrackingProfileService.ActiveProfileFileName);
                    try
                    {
                        if (File.Exists(activeProfilePath))
                        {
                            var fileName = (await File.ReadAllTextAsync(activeProfilePath)).Trim();
                            if (fileName != TrackingProfileService.DefaultProfileValue && Path.GetFileName(fileName) == fileName)
                            {
                                var profilePath = Path.Combine(profilesPath, fileName);
                                if (File.Exists(profilePath))
                                {
                                    TrackingProfileService.ApplyProfile(_settings, fileName, reader.Read(profilePath));
                                }
                            }
                        }
                    }
                    catch (IOException) { }
                    catch (UnauthorizedAccessException) { }
                }

                // Resolve one connection string for migrations, live writes, and historical UI reads.
                var connectionString = builder.Configuration.GetConnectionString("BaseStationReaderDB")
                    ?? throw new InvalidOperationException("Connection string 'BaseStationReaderDB' is not configured.");
                var contextOptions = new DbContextOptionsBuilder<BaseStationReaderDbContext>()
                    .UseSqlite(connectionString)
                    .Options;

                // Make sure the latest migrations have been applied - this ensures the DB is created and in the
                // correct state if it's absent or stale on startup
                using var context = new BaseStationReaderDbContext(contextOptions);
                context.Database.Migrate();
                context.Database.ExecuteSqlRaw("PRAGMA journal_mode=WAL;");
                context.Database.ExecuteSqlRaw("PRAGMA busy_timeout=5000;");
                _logger.LogMessage(Severity.Debug, "Latest database migrations have been applied");

                // Initialise the tracker wrapper
                var runtime = new TrackingRuntime(_settings, (settings, notes) => new TrackerController(
                    _logger,
                    new BaseStationReaderDbContext(contextOptions),
                    new TrackerTcpClient(),
                    settings,
                    ownsContext: true,
                    sessionNotes: notes));
                _controller = runtime;

                // Bind Kestrel options from the applicatiokn settings file
                builder.WebHost.ConfigureKestrel(options =>
                {
                    builder.Configuration.GetSection("Kestrel").Bind(options);
                });

                // Register SignalR
                builder.Services.AddSignalR().AddMessagePackProtocol();
                builder.Services.AddResponseCompression(o => o.EnableForHttps = true);
                builder.Services.AddRazorComponents().AddInteractiveServerComponents();
                // This cache is strictly process memory: it has no database, distributed, or file-backed provider.
                builder.Services.AddSingleton<ITransientResponseCache, MemoryOnlyTransientResponseCache>();
                // Scoped UI state lives only in one Blazor circuit and has no persistent storage backing.
                builder.Services.AddScoped<ITrackerHubPageState, TrackerHubPageState>();
                builder.Services.AddScoped<ILiveAircraftService, LiveAircraftService>();
                builder.Services.AddSingleton(defaultSettings);
                builder.Services.AddSingleton(
                    builder.Configuration.GetSection("ApplicationSettings").Get<ExternalApiSettings>() ?? new());
                builder.Services.AddSingleton(runtime);
                builder.Services.AddSingleton<ITrackingProfileService, TrackingProfileService>();
                builder.Services.AddSingleton<ITrackingControlService, TrackingControlService>();
                builder.Services.Configure<RadarOptions>(builder.Configuration.GetSection("WebUi:Radar"));
                builder.Services.Configure<DatabaseBrowserOptions>(builder.Configuration.GetSection("WebUi:Database"));
                builder.Services.Configure<ScheduleOptions>(builder.Configuration.GetSection("ApplicationSettings"));
                builder.Services.AddPooledDbContextFactory<BaseStationReaderDbContext>(options =>
                    options.UseSqlite(connectionString));
                builder.Services.AddSingleton<IReceiverPositionProvider>(runtime);
                builder.Services.AddSingleton<IFlightProfileBuilder>(new FlightProfileBuilder(runtime));
                builder.Services.AddSingleton<IFlightPathBuilder>(new FlightPathBuilder(runtime));
                builder.Services.AddSingleton<IRadarProjectionService>(new RadarProjectionService(runtime));
                builder.Services.AddScoped<ITrackingSessionQueryService, TrackingSessionQueryService>();
                builder.Services.AddScoped<ITrackingSessionQueryManager, TrackingSessionQueryManager>();
                builder.Services.AddScoped<ILiveTrackerStatusService, LiveTrackerStatusService>();
                builder.Services.AddScoped<IObservationSessionEditorService, ObservationSessionEditorService>();
                builder.Services.AddScoped<IAirportWeatherLookupService, AirportWeatherLookupService>();
                builder.Services.AddScoped<IAirportScheduleLookupService, AirportScheduleLookupService>();
                builder.Services.AddScoped<IAirportRouteService, AirportRouteService>();
                builder.Services.AddScoped<IReferenceLookupService, ReferenceLookupService>();
                builder.Services.AddSingleton<IDataImportService, DataImportService>();
                builder.Services.AddScoped<IProvenanceService, ProvenanceService>();
                builder.Services.AddScoped<IAircraftReferenceService, AircraftReferenceService>();
                builder.Services.AddScoped<IAirlineReferenceService, AirlineReferenceService>();
                builder.Services.AddScoped<IAirportReferenceService, AirportReferenceService>();
                builder.Services.AddScoped<IFlightReferenceService, FlightReferenceService>();
                builder.Services.AddScoped<IManufacturerReferenceService, ManufacturerReferenceService>();
                builder.Services.AddScoped<IModelReferenceService, ModelReferenceService>();
                builder.Services.AddHttpClient<IMapboxStaticMapService, MapboxStaticMapService>(client =>
                    client.Timeout = TimeSpan.FromSeconds(30));

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

                // Serve the unified UI assets; the legacy standalone index page has been retired.
                var provider = new FileExtensionContentTypeProvider();
                provider.Mappings[".map"] = "application/json";
                app.UseStaticFiles(new StaticFileOptions
                {
                    ContentTypeProvider = provider
                });
                
                app.UseHttpsRedirection();
                app.UseAntiforgery();

                // Map the existing external hub and the new interactive server UI.
                app.MapHub<AircraftHub>("/hubs/aircraft");
                app.MapRazorComponents<App>().AddInteractiveServerRenderMode();
                app.MapGet("/api/mapbox/static", async (
                    double north,
                    double south,
                    double east,
                    double west,
                    IMapboxStaticMapService mapbox,
                    CancellationToken token) =>
                {
                    // Reject invalid or inverted bounds before attempting a remote Mapbox request.
                    if (!double.IsFinite(north) || !double.IsFinite(south) ||
                        !double.IsFinite(east) || !double.IsFinite(west) ||
                        north <= south || east <= west ||
                        north > 90 || south < -90 || east > 180 || west < -180)
                    {
                        return Results.BadRequest();
                    }

                    var image = await mapbox.GetMapAsync(north, south, east, west, token);
                    return image is null ? Results.NotFound() : Results.File(image, "image/png");
                });

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
                    Console.WriteLine($"Processing {_controller.QueueSize} pending database updates ...");
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
                    if (((TrackingRuntime)_controller).IsTracking &&
                        (_settings.ApplicationTimeout > 0) && (elapsed > _settings.ApplicationTimeout))
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

    }
}
