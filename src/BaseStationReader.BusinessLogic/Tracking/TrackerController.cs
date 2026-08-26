using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Geometry;
using BaseStationReader.BusinessLogic.Messages;
using BaseStationReader.BusinessLogic.Spool;
using System.Collections.Concurrent;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Messages;
using BaseStationReader.BusinessLogic.Events;
using BaseStationReader.Interfaces.Geometry;
using BaseStationReader.Interfaces.Events;
using BaseStationReader.Interfaces.Spool;
using BaseStationReader.Entities.Hub;
using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Spool;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Tracking
{
    public class TrackerController : ITrackerController
    {
        private readonly IControllerNotificationSender _sender;
        private readonly IDatabaseManagementFactory _factory;
        private readonly TrackerApplicationSettings _settings;
        private readonly BaseStationReaderDbContext _context;
        private readonly bool _ownsContext;
        private readonly string _sessionNotes;
        private readonly string _sessionName;
        private readonly ITrackerLogger _logger;
        private readonly ITrackerTcpClient _tcpClient;
        private readonly IPositionDensitySnapshotOrchestrator _densityOrchestrator;
        private readonly IPositionDensitySnapshotMapper _densitySnapshotMapper;
        private IAircraftTracker _tracker = null;
        private IMessageReader _reader = null;
        private IContinuousWriter _writer = null;
        private ObservationSession _activeSession = null;
        private bool? _stopFlushOverride;
        private CancellationToken _stopFlushCancellationToken;
        private IProgress<QueueFlushProgress> _stopFlushProgress;
        private int _remainingQueueSize;
        private bool _writerStopped;
        private readonly ConcurrentDictionary<string, string> _addedSinceSummary = new(StringComparer.OrdinalIgnoreCase);
        private readonly ConcurrentDictionary<string, string> _removedSinceSummary = new(StringComparer.OrdinalIgnoreCase);
        private CancellationTokenSource _summarySource;
        private Task _summaryTask;
        private long _lastSummaryMessages;
        private long _lastSummaryPositionsObserved;
        private long _lastSummaryPositionsWritten;

        private const int MaximumAircraftListedInSummary = 20;

        public event EventHandler<AircraftNotificationEventArgs> AircraftEvent;
        public event EventHandler<MessageReadEventArgs> MessageReceived;

        private readonly ConcurrentDictionary<string, TrackedAircraft> _trackedAircraft = new();
        private readonly ConcurrentDictionary<string, byte> _positionAircraftObserved = new(StringComparer.OrdinalIgnoreCase);
        private long _positionRecordsObserved;

        public IEnumerable<TrackedAircraftDto> State
        {
            get
            {
                var snapshot = _trackedAircraft.Values.ToList();
                return snapshot.Select(TrackedAircraftDto.FromTrackedAircraft);
            }
        }

        public TrackingOptions TrackingOptions => TrackingOptions.FromTrackerSettings(_settings);

        /// <inheritdoc />
        public long MessagesProcessed => _tracker?.MessagesProcessed ?? 0;

        /// <inheritdoc />
        public long PositionRecordsWritten => _writer?.PositionRecordsWritten ?? 0;

        /// <inheritdoc />
        public long PositionRecordsObserved => Interlocked.Read(ref _positionRecordsObserved);

        /// <inheritdoc />
        /// <inheritdoc />
        public long DistinctAircraft => _tracker?.DistinctAircraft ?? 0;

        /// <inheritdoc />
        public long DistinctCallsigns => _tracker?.DistinctCallsigns ?? 0;

        /// <inheritdoc />
        public long AircraftWithPositionRecords => _writer?.AircraftWithPositionRecords ?? 0;

        /// <inheritdoc />
        public long AircraftWithPositionObservations => _positionAircraftObserved.Count;

        /// <summary>
        /// Initialises a controller; asynchronous tracking dependencies are loaded when tracking starts.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="context"></param>
        /// <param name="tcpClient"></param>
        /// <param name="settings"></param>
        /// <param name="ownsContext"></param>
        /// <param name="sessionNotes"></param>
        /// <param name="sessionName"></param>
        /// <param name="densityOrchestrator"></param>
        /// <param name="densitySnapshotMapper"></param>
        /// <param name="spoolQueue">Optional persistent queue supplied by the composition root.</param>
        public TrackerController(
            ITrackerLogger logger,
            BaseStationReaderDbContext context,
            ITrackerTcpClient tcpClient,
            TrackerApplicationSettings settings,
            bool ownsContext = false,
            string sessionNotes = null,
            string sessionName = null,
            IPositionDensitySnapshotOrchestrator densityOrchestrator = null,
            IPositionDensitySnapshotMapper densitySnapshotMapper = null,
            ISpoolQueue spoolQueue = null)
        {
            _settings = settings;
            _context = context;
            _ownsContext = ownsContext;
            _sessionNotes = string.IsNullOrWhiteSpace(sessionNotes) ? null : sessionNotes.Trim();
            _sessionName = string.IsNullOrWhiteSpace(sessionName) ? null : sessionName.Trim();
            _logger = logger;
            _tcpClient = tcpClient;
            _densityOrchestrator = densityOrchestrator;
            _densitySnapshotMapper = densitySnapshotMapper;

            // Configure the database management classes
            _factory = new DatabaseManagementFactory(logger, context, _settings.TimeToLock);

            // Configure the SQL writer, if enabled
            if (_settings.EnableSqlWriter)
            {
                if (spoolQueue is null)
                {
                    var connectionString = context.Database.GetConnectionString()
                        ?? throw new InvalidOperationException("The database connection string is required for the writer spool.");
                    var spoolFolder = SpoolFolderResolver.Resolve(connectionString, _settings.SpoolFolder);
                    spoolQueue = new SpoolQueueManager(spoolFolder);
                }

                _writer = new ContinuousWriter(
                    _factory,
                    spoolQueue,
                    _settings.FlushOnStop,
                    _settings.FlushWhileActive);
            }

            // Create the controller notification sender
            _sender = new ControllerNotificationSender(logger);
        }

        /// <summary>
        /// Start tracking aircraft
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task StartAsync(CancellationToken token)
        {
            if (_settings.TrackPosition && _settings.TrackPositionDensity && _settings.PositionDensityInterval <= 0)
            {
                throw new InvalidOperationException("PositionDensityInterval must be greater than zero when position-density tracking is enabled.");
            }

            _tracker ??= await CreateTrackerAsync(token);
            _reader.MessageRead += OnRawMessageReceived;

            // Persist the session before tracking begins so every subsequent aircraft record can refer to it.
            if (_writer != null)
            {
                _activeSession = CreateObservationSession();
                await _factory.ObservationSessionManager.AddAsync(_activeSession, token);
            }

            // Attach the aircraft tracking event handlers
            _tracker.AircraftEvent += OnAircraftEvent;

            // Start the queued writer
            if (_writer != null)
            {
                await _writer.StartAsync(token);
            }

            StartTrackingSummary(token);

            if (_activeSession is not null && _settings.TrackPosition && _settings.TrackPositionDensity)
            {
                // The controller owns the periodic lifecycle so every host follows identical session boundaries.
                var bounds = PositionDensityBoundsFactory.Create(
                    _settings.ReceiverLatitude,
                    _settings.ReceiverLongitude,
                    _settings.MaximumTrackedDistance);
                _densityOrchestrator?.Start(
                    _activeSession.Id,
                    bounds,
                    TimeSpan.FromMilliseconds(_settings.PositionDensityInterval),
                    QueuePositionDensitySnapshot,
                    token);
            }

            try
            {
                // Start the aircraft tracker
                await _tracker.StartAsync(token);
            }
            catch (TaskCanceledException)
            {
                // Expected when the token is cancelled
                throw;
            }
            finally
            {
                try
                {
                    await StopTrackingSummaryAsync();

                    // Release persistence first. RequestStop has already closed the producer boundary,
                    // so a density callback racing with shutdown cannot add another record. This keeps
                    // unrelated density cleanup from retaining the exclusive spool lock.
                    if (_writer != null)
                    {
                        try
                        {
                            await _writer.StopAsync(
                                _stopFlushOverride,
                                _stopFlushCancellationToken,
                                _stopFlushProgress);
                        }
                        finally
                        {
                            _remainingQueueSize = _writer.QueueSize;
                            _writerStopped = true;
                            await _writer.DisposeAsync();
                        }
                    }

                    LogTrackingSummary(final: true);
                }
                finally
                {
                    try
                    {
                        // No periodic density work may outlive the completed tracking session.
                        if (_densityOrchestrator is not null)
                        {
                            await _densityOrchestrator.StopAsync();
                        }
                    }
                    finally
                    {
                        // Detach the aircraft tracking event handlers
                        _tracker.AircraftEvent -= OnAircraftEvent;
                        _reader.MessageRead -= OnRawMessageReceived;

                        // A stopped controller must not associate any later events with the completed tracking run.
                        _activeSession = null;

                        if (_ownsContext)
                        {
                            await _context.DisposeAsync();
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Return the number of pending requests in the writer queue
        /// </summary>
        public int QueueSize => _writerStopped ? _remainingQueueSize : _writer?.QueueSize ?? 0;

        /// <summary>
        /// Process all pending entries in the queued writer queue
        /// </summary>
        /// <returns></returns>
        public async Task FlushQueueAsync(CancellationToken cancellationToken = default,
            IProgress<QueueFlushProgress> progress = null)
        {
            if (_writer != null)
            {
                await _writer.FlushQueueAsync(cancellationToken, progress);
            }
        }

        /// <inheritdoc />
        public void ConfigureStopFlush(bool flushQueue, CancellationToken cancellationToken = default,
            IProgress<QueueFlushProgress> progress = null)
        {
            _stopFlushOverride = flushQueue;
            _stopFlushCancellationToken = cancellationToken;
            _stopFlushProgress = progress;
        }

        /// <inheritdoc />
        public void RequestStop()
        {
            // Stop accepting spool entries and cancel an in-flight database call before the
            // receiver and density loops begin their asynchronous shutdown.
            _writer?.RequestStop();
        }

        /// <summary>
        /// Creates the aircraft tracker after asynchronously loading the current exclusion lists.
        /// </summary>
        private async Task<IAircraftTracker> CreateTrackerAsync(CancellationToken token)
        {
            token.ThrowIfCancellationRequested();
            var excludedAddresses = (await _factory.ExcludedAddressManager.ListAsync(x => true))
                .Select(item => item.Address)
                .ToList();
            var excludedCallsigns = (await _factory.ExcludedCallsignManager.ListAsync(x => true))
                .Select(item => item.Callsign)
                .ToList();

            var readerSender = new MessageReaderNotificationSender(_logger);
            _reader = new MessageReader(
                _tcpClient,
                _logger,
                readerSender,
                _settings.Host,
                _settings.Port,
                _settings.SocketReadTimeout);
            var parsers = new Dictionary<MessageType, IMessageParser>
            {
                { MessageType.MSG, new MsgMessageParser() }
            };

            IDistanceCalculator distanceCalculator = null;
            if (_settings.ReceiverLatitude != null && _settings.ReceiverLongitude != null)
            {
                distanceCalculator = new ReceiverDistanceCalculator(new GeographicCalculator())
                {
                    ReferenceLatitude = _settings.ReceiverLatitude.Value,
                    ReferenceLongitude = _settings.ReceiverLongitude.Value
                };
            }

            var assessor = new SimpleAircraftBehaviourAssessor();
            var propertyUpdater = new AircraftPropertyUpdater(_logger, distanceCalculator, assessor);
            var trackerSender = new AircraftTrackerNotificationSender(
                _logger,
                _settings.TrackedBehaviours,
                _settings.MaximumTrackedDistance,
                _settings.MinimumTrackedAltitude,
                _settings.MaximumTrackedAltitude);

            return new AircraftTracker(
                _reader,
                parsers,
                propertyUpdater,
                trackerSender,
                excludedAddresses,
                excludedCallsigns,
                _settings.TimeToRecent,
                _settings.TimeToStale,
                _settings.TimeToRemoval);
        }

        /// <summary>Forwards an unfiltered feed heartbeat independently of aircraft notification criteria.</summary>
        private void OnRawMessageReceived(object sender, MessageReadEventArgs args)
            => MessageReceived?.Invoke(this, args);

        /// <summary>
        /// Create a historical snapshot of the effective tracking profile for the new run
        /// </summary>
        /// <returns></returns>
        private ObservationSession CreateObservationSession()
        {
            // Named profiles take precedence; the configured default supplies a meaningful name otherwise.
            var profileName = string.IsNullOrWhiteSpace(_settings.TrackingProfileName)
                ? _settings.DefaultProfileName
                : _settings.TrackingProfileName;

            var startedAtUtc = DateTime.UtcNow;
            return new ObservationSession
            {
                Name = _sessionName ?? startedAtUtc.ToString("yyyy-MM-dd HH:mm:ss"),
                StartedAtUtc = startedAtUtc,
                ProfileName = profileName,
                Notes = _sessionNotes,
                Host = _settings.Host,
                Port = _settings.Port,
                ReceiverLatitude = _settings.ReceiverLatitude,
                ReceiverLongitude = _settings.ReceiverLongitude,
                ReceiverElevation = _settings.ReceiverElevation,
                MinimumAltitude = _settings.MinimumTrackedAltitude,
                MaximumAltitude = _settings.MaximumTrackedAltitude,
                MaximumDistance = _settings.MaximumTrackedDistance,
                IncludedBehaviours = string.Join(",", _settings.TrackedBehaviours)
            };
        }

        /// <summary>
        /// Handle aircraft events
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAircraftEvent(object sender, AircraftNotificationEventArgs e)
        {
            var isRemoval = e.NotificationType == AircraftNotificationType.Removed;

            // Keep the live snapshot current for every accepted message. Persistence retains its
            // independent notification interval, but display consumers must never see an old state.
            if (isRemoval)
            {
                _trackedAircraft.Remove(e.Aircraft.Address, out TrackedAircraft _);
            }
            else
            {
                _trackedAircraft[e.Aircraft.Address] = (TrackedAircraft)e.Aircraft.Clone();
            }

            if (e.NotificationType == AircraftNotificationType.Added)
            {
                _addedSinceSummary[e.Aircraft.Address] = e.Aircraft.Callsign ?? "";
                _logger.LogMessage(Severity.Debug, FormatAircraftLifecycle("Detected", e.Aircraft));
            }
            else if (isRemoval)
            {
                _removedSinceSummary[e.Aircraft.Address] = e.Aircraft.Callsign ?? "";
                _logger.LogMessage(Severity.Debug, FormatAircraftLifecycle("Removed", e.Aircraft));
            }
            else if (e.NotificationType is AircraftNotificationType.Recent or AircraftNotificationType.Stale)
            {
                _logger.LogMessage(Severity.Debug, FormatAircraftLifecycle($"Status changed to {e.Aircraft.Status}", e.Aircraft));
            }

            if (ShouldNotify(e.Aircraft))
            {
                e.Aircraft.LastNotified = DateTime.Now;
                var position = e.MeetsTrackingCriteria ? e.Position : null;
                HandleAircraftEvent(e.Aircraft, position);

            }

            // Display consumers coalesce these events at their own refresh intervals.
            _sender.SendAircraftNotification(e.Aircraft, e.MeetsTrackingCriteria ? e.Position : null,
                this, e.NotificationType, AircraftEvent);
        }

        /// <summary>
        /// Return true if a notification should be sent for a specified aircraft and updates pushed to the
        /// queue for writing/processing
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private bool ShouldNotify(TrackedAircraft aircraft)
        {
            // If it's never notified before, send the notification
            if (aircraft.LastNotified == null)
            {
                return true;
            }

            // Calculate the time since the last notification and notify if the aircraft notification interval
            // has been reached
            var elapsed = (DateTime.Now - aircraft.LastNotified.Value).TotalMilliseconds;
            return elapsed >= _settings.AircraftNotificationInterval;
        }

        /// <summary>
        /// Handle an aircraft addition or removal event
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="position"></param>
        private void HandleAircraftEvent(TrackedAircraft aircraft, AircraftPosition position)
        {
            // Every observation belongs to the session created before tracking starts. Treat a missing session
            // as an invalid lifecycle state rather than allowing unscoped data into the persistence queue.
            var sessionId = _activeSession?.Id
                ?? throw new InvalidOperationException("Aircraft observations cannot be queued without an active session.");
            aircraft.SessionId = sessionId;
            if (position != null)
            {
                // Positions are captured before this event reaches the session-aware controller, so add the
                // in-memory routing value here before the position crosses the asynchronous writer boundary.
                position.SessionId = sessionId;
                Interlocked.Increment(ref _positionRecordsObserved);
                _positionAircraftObserved.TryAdd(aircraft.Address, 0);
            }

            // Push the aircraft and its position to the SQL writer, if enabled
            if (_writer != null)
            {
                // Push the aircraft to the writer queue
                _factory.Logger.LogMessage(Severity.Verbose, $"Queueing aircraft {aircraft.Address} {aircraft.Behaviour} for writing");
                _writer.Push(aircraft);

                // Push the aircraft position to the writer queue
                if (position != null)
                {
                    _factory.Logger.LogMessage(Severity.Verbose, $"Queueing position with ID {position.Id} for aircraft {aircraft.Address} {aircraft.Behaviour} for writing");
                    _writer.Push(position);
                }
            }

            // Density input follows the same accepted position events but remains independent of persistence.
            _densityOrchestrator?.Record(position);
        }

        /// <summary>
        /// Maps and queues a regenerated snapshot behind previously accepted position writes.
        /// </summary>
        /// <param name="snapshot"></param>
        /// <param name="capturedAtUtc"></param>
        private void QueuePositionDensitySnapshot(PositionDensity snapshot, DateTime capturedAtUtc)
        {
            if (_writer is null || _densitySnapshotMapper is null)
            {
                return;
            }

            // Mapping creates an independent immutable request before it crosses the asynchronous queue boundary.
            _writer.Push(_densitySnapshotMapper.Map(snapshot, capturedAtUtc));
        }

        /// <summary>Starts the shared operational summary loop for this tracking session.</summary>
        private void StartTrackingSummary(CancellationToken token)
        {
            _lastSummaryMessages = MessagesProcessed;
            _lastSummaryPositionsObserved = PositionRecordsObserved;
            _lastSummaryPositionsWritten = PositionRecordsWritten;
            _logger.LogMessage(
                Severity.Info,
                $"Tracking session started: profile={TrackingOptions.TrackingProfileName}, " +
                $"feed={_settings.Host}:{_settings.Port}, sqlWriter={_settings.EnableSqlWriter}, " +
                $"flushWhileActive={_settings.FlushWhileActive}, flushOnStop={_settings.FlushOnStop}");

            if (_settings.TrackingLogSummaryInterval <= 0)
            {
                return;
            }

            _summarySource = CancellationTokenSource.CreateLinkedTokenSource(token);
            _summaryTask = RunTrackingSummaryAsync(
                TimeSpan.FromMilliseconds(_settings.TrackingLogSummaryInterval),
                _summarySource.Token);
        }

        /// <summary>Writes operational summaries until the tracking session ends.</summary>
        private async Task RunTrackingSummaryAsync(TimeSpan interval, CancellationToken token)
        {
            using var timer = new PeriodicTimer(interval);
            try
            {
                while (await timer.WaitForNextTickAsync(token).ConfigureAwait(false))
                {
                    LogTrackingSummary(final: false);
                }
            }
            catch (OperationCanceledException) when (token.IsCancellationRequested)
            {
                // Expected when the session ends.
            }
        }

        /// <summary>Stops the periodic summary loop before the final snapshot is written.</summary>
        private async Task StopTrackingSummaryAsync()
        {
            if (_summaryTask is null)
            {
                return;
            }

            _summarySource.Cancel();
            await _summaryTask.ConfigureAwait(false);
            _summarySource.Dispose();
            _summarySource = null;
            _summaryTask = null;
        }

        /// <summary>Writes one interval or final tracker-health snapshot.</summary>
        private void LogTrackingSummary(bool final)
        {
            var messages = MessagesProcessed;
            var positionsObserved = PositionRecordsObserved;
            var positionsWritten = PositionRecordsWritten;
            var intervalMessages = messages - Interlocked.Exchange(ref _lastSummaryMessages, messages);
            var intervalPositionsObserved = positionsObserved - Interlocked.Exchange(ref _lastSummaryPositionsObserved, positionsObserved);
            var intervalPositionsWritten = positionsWritten - Interlocked.Exchange(ref _lastSummaryPositionsWritten, positionsWritten);
            var state = _trackedAircraft.Values.ToList();
            var added = DrainSummaryAircraft(_addedSinceSummary);
            var removed = DrainSummaryAircraft(_removedSinceSummary);

            _logger.LogMessage(
                Severity.Info,
                $"Tracking {(final ? "final summary" : "summary")}: " +
                $"live={state.Count} (active={state.Count(x => x.Status == TrackingStatus.Active)}, " +
                $"inactive={state.Count(x => x.Status == TrackingStatus.Inactive)}, " +
                $"stale={state.Count(x => x.Status == TrackingStatus.Stale)}); " +
                $"interval messages={intervalMessages:N0}, positions observed={intervalPositionsObserved:N0}, " +
                $"positions written={intervalPositionsWritten:N0}, added={added.Count:N0}, removed={removed.Count:N0}; " +
                $"session messages={messages:N0}, distinct aircraft={DistinctAircraft:N0}, " +
                $"distinct callsigns={DistinctCallsigns:N0}, positions observed={positionsObserved:N0}, " +
                $"positions written={positionsWritten:N0}, queue={QueueSize:N0}");

            if (added.Count > 0 || removed.Count > 0)
            {
                _logger.LogMessage(
                    Severity.Info,
                    $"Aircraft changes: added={FormatSummaryAircraft(added)}; removed={FormatSummaryAircraft(removed)}");
            }
        }

        /// <summary>Atomically claims accumulated lifecycle changes for one summary.</summary>
        private static List<KeyValuePair<string, string>> DrainSummaryAircraft(
            ConcurrentDictionary<string, string> aircraft)
        {
            var drained = new List<KeyValuePair<string, string>>();
            foreach (var entry in aircraft)
            {
                if (aircraft.TryRemove(entry.Key, out var callsign))
                {
                    drained.Add(new KeyValuePair<string, string>(entry.Key, callsign));
                }
            }

            return drained.OrderBy(x => x.Key, StringComparer.OrdinalIgnoreCase).ToList();
        }

        /// <summary>Formats a bounded lifecycle list while retaining its accurate total.</summary>
        private static string FormatSummaryAircraft(IReadOnlyCollection<KeyValuePair<string, string>> aircraft)
        {
            if (aircraft.Count == 0)
            {
                return "0 []";
            }

            var displayed = aircraft.Take(MaximumAircraftListedInSummary)
                .Select(x => string.IsNullOrWhiteSpace(x.Value) ? x.Key : $"{x.Key}/{x.Value.Trim()}");
            var omitted = aircraft.Count - MaximumAircraftListedInSummary;
            var suffix = omitted > 0 ? $", +{omitted:N0} more" : "";
            return $"{aircraft.Count:N0} [{string.Join(", ", displayed)}{suffix}]";
        }

        private static string FormatAircraftLifecycle(string action, TrackedAircraft aircraft)
            => string.IsNullOrWhiteSpace(aircraft.Callsign)
                ? $"Aircraft {action.ToLowerInvariant()}: {aircraft.Address}"
                : $"Aircraft {action.ToLowerInvariant()}: {aircraft.Address}/{aircraft.Callsign.Trim()}";
    }
}
