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
using System.Collections.Concurrent;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Messages;
using BaseStationReader.BusinessLogic.Events;
using BaseStationReader.Interfaces.Geometry;
using BaseStationReader.Interfaces.Events;
using BaseStationReader.Entities.Hub;
using BaseStationReader.Entities.History;

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
        private IContinuousWriter _writer = null;
        private ObservationSession _activeSession = null;

        public event EventHandler<AircraftNotificationEventArgs> AircraftEvent;

        private readonly ConcurrentDictionary<string, TrackedAircraft> _trackedAircraft = new();

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
        public long AircraftAdded => _tracker?.AircraftAdded ?? 0;

        /// <inheritdoc />
        public long AircraftRemoved => _tracker?.AircraftRemoved ?? 0;

        /// <inheritdoc />
        public long DistinctAircraft => _tracker?.DistinctAircraft ?? 0;

        /// <inheritdoc />
        public long DistinctCallsigns => _tracker?.DistinctCallsigns ?? 0;

        /// <inheritdoc />
        public long AircraftWithPositionRecords => _writer?.AircraftWithPositionRecords ?? 0;

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
        public TrackerController(
            ITrackerLogger logger,
            BaseStationReaderDbContext context,
            ITrackerTcpClient tcpClient,
            TrackerApplicationSettings settings,
            bool ownsContext = false,
            string sessionNotes = null,
            string sessionName = null,
            IPositionDensitySnapshotOrchestrator densityOrchestrator = null,
            IPositionDensitySnapshotMapper densitySnapshotMapper = null)
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
                _writer = new ContinuousWriter(_factory);
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

            // If the queued writer is enabled and clear-down is configured, clear down previous
            // tracking data
            if ((_writer != null) && _settings.ClearDown)
            {
                await _factory.Context<BaseStationReaderDbContext>()?.ClearDown();
            }

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
                    if (_densityOrchestrator is not null)
                    {
                        await _densityOrchestrator.StopAsync();
                    }
                }
                finally
                {
                    // Always drain and dispose persistence even if snapshot generation itself failed.
                    if (_writer != null)
                    {
                        await _writer.StopAsync();
                        await _writer.DisposeAsync();
                    }

                    // Detach the aircraft tracking event handlers
                    _tracker.AircraftEvent -= OnAircraftEvent;

                    // A stopped controller must not associate any later events with the completed tracking run.
                    _activeSession = null;

                    if (_ownsContext)
                    {
                        await _context.DisposeAsync();
                    }
                }
            }
        }

        /// <summary>
        /// Return the number of pending requests in the writer queue
        /// </summary>
        public int QueueSize => _writer?.QueueSize ?? 0;

        /// <summary>
        /// Process all pending entries in the queued writer queue
        /// </summary>
        /// <returns></returns>
        public async Task FlushQueueAsync()
        {
            if (_writer != null)
            {
                await _writer.FlushQueueAsync();
            }
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
            var reader = new MessageReader(
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
                reader,
                parsers,
                propertyUpdater,
                trackerSender,
                excludedAddresses,
                excludedCallsigns,
                _settings.TimeToRecent,
                _settings.TimeToStale,
                _settings.TimeToRemoval);
        }

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

            if (ShouldNotify(e.Aircraft))
            {
                e.Aircraft.LastNotified = DateTime.Now;
                var position = e.MeetsTrackingCriteria ? e.Position : null;
                HandleAircraftEvent(e.Aircraft, position);

                // HandleAircraftEvent persists the final state, but removed aircraft must not remain
                // in the authoritative live snapshot exposed by the Tracker Hub.
                if (isRemoval)
                {
                    _trackedAircraft.Remove(e.Aircraft.Address, out TrackedAircraft _);
                }

                _sender.SendAircraftNotification(e.Aircraft, position, this, e.NotificationType, AircraftEvent);
            }
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
            }

            // The live table represents every detected aircraft; position criteria affect only the optional
            // position queued alongside it.
            var existingAircraft = _trackedAircraft.ContainsKey(aircraft.Address);
            if (!existingAircraft)
            {
                _trackedAircraft[aircraft.Address] = (TrackedAircraft)aircraft.Clone();
            }
            else
            {
                _trackedAircraft[aircraft.Address] = aircraft;
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
    }
}
