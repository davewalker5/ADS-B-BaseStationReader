using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Maths;
using BaseStationReader.BusinessLogic.Messages;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.BusinessLogic.Tracking
{
    [ExcludeFromCodeCoverage]
    public class TrackerWrapper : ITrackerWrapper
    {
        private readonly ITrackerLogger _logger;
        private IAircraftTracker _tracker = null;
        private readonly TrackerApplicationSettings _settings;
        private IQueuedWriter _writer = null;

        public event EventHandler<AircraftNotificationEventArgs> AircraftAdded;
        public event EventHandler<AircraftNotificationEventArgs> AircraftUpdated;
        public event EventHandler<AircraftNotificationEventArgs> AircraftRemoved;

        public ConcurrentDictionary<string, TrackedAircraft> TrackedAircraft { get; private set; } = new();
        public bool IsTracking { get { return (_tracker != null) && _tracker.IsTracking; } }

        public TrackerWrapper(ITrackerLogger logger, TrackerApplicationSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        /// <summary>
        /// Initialise the tracking and writing system
        /// </summary>
        public async Task InitialiseAsync()
        {
            // Set up the message reader and parser
            var reader = new MessageReader(_logger, _settings.Host, _settings.Port, _settings.SocketReadTimeout);
            var parsers = new Dictionary<MessageType, IMessageParser>
            {
                { MessageType.MSG, new MsgMessageParser() }
            };

            // Set up a distance calculator, if the receiver's latitude and longitude have been supplied
            IDistanceCalculator distanceCalculator = null;
            if ((_settings.ReceiverLatitude != null) && (_settings.ReceiverLongitude != null))
            {
                distanceCalculator = new HaversineCalculator
                {
                    ReferenceLatitude = _settings.ReceiverLatitude ?? 0,
                    ReferenceLongitude = _settings.ReceiverLongitude ?? 0
                };
            }

            // Set up the aircraft tracker
            var trackerTimer = new TrackerTimer(_settings.TimeToRecent / 10.0);
            var assessor = new SimpleAircraftBehaviourAssessor();
            var propertyUpdater = new AircraftPropertyUpdater(_logger, distanceCalculator, assessor);

            var notificationSender = new NotificationSender(
                _logger,
                _settings.TrackedBehaviours,
                _settings.MaximumTrackedDistance,
                _settings.MinimumTrackedAltitude,
                _settings.MaximumTrackedAltitude);

            _tracker = new AircraftTracker(reader,
                parsers,
                trackerTimer,
                propertyUpdater,
                notificationSender,
                _settings.TimeToRecent,
                _settings.TimeToStale,
                _settings.TimeToRemoval);

            // Wire up the aircraft tracking events
            _tracker.AircraftAdded += OnAircraftAdded;
            _tracker.AircraftUpdated += OnAircraftUpdated;
            _tracker.AircraftRemoved += OnAircraftRemoved;

            // Set up the queued database writer
            if (_settings.EnableSqlWriter)
            {
                BaseStationReaderDbContext context = new BaseStationReaderDbContextFactory().CreateDbContext(Array.Empty<string>());
                var aircraftWriter = new TrackedAircraftWriter(context);
                var positionWriter = new PositionWriter(context);
                var aircraftLocker = new AircraftLockManager(aircraftWriter, _settings.TimeToLock);
                var writerTimer = new TrackerTimer(_settings.WriterInterval);
                _writer = new QueuedWriter(aircraftWriter, positionWriter, aircraftLocker, _logger!, writerTimer, _settings.WriterBatchSize);
                _writer.BatchWritten += OnBatchWritten;

                // If instructed, clear down aircraft tracking data while leaving aircraft details and airlines intact
                if (_settings.ClearDown)
                {
                    await context.ClearDown();
                }

                await _writer.StartAsync();
            }
        }

        /// <summary>
        /// Start reading messages
        /// </summary>
        public void Start()
            => _tracker!.Start();

        /// <summary>
        /// Stop reading messages
        /// </summary>
        public void Stop()
            => _tracker!.Stop();

        /// <summary>
        /// Handle the event raised when a new aircraft is detected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAircraftAdded(object sender, AircraftNotificationEventArgs e)
        {
            // Add the aircraft to the collection
            TrackedAircraft[e.Aircraft.Address] = (TrackedAircraft)e.Aircraft.Clone();

            // Push the aircraft and its position to the SQL writer, if enabled
            if (_writer != null)
            {
                _logger.LogMessage(Severity.Debug, $"Queueing aircraft {e.Aircraft.Address} {e.Aircraft.Behaviour} for writing");
                _writer.Push(e.Aircraft);

                if (e.Position != null)
                {
                    _logger.LogMessage(Severity.Debug, $"Queueing position with ID {e.Position.Id} for aircraft {e.Aircraft.Address} {e.Aircraft.Behaviour} for writing");
                    _writer.Push(e.Position);
                }
            }

            // Forward the event to subscribers
            AircraftAdded?.Invoke(this, e);
        }

        /// <summary>
        /// Handle the event raised when an existing aircraft is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAircraftUpdated(object sender, AircraftNotificationEventArgs e)
        {
            // If the aircraft isn't already in the collection, add it. Otherwise, update its entry
            if (!TrackedAircraft.ContainsKey(e.Aircraft.Address))
            {
                TrackedAircraft[e.Aircraft.Address] = (TrackedAircraft)e.Aircraft.Clone();
            }
            else
            {
                TrackedAircraft[e.Aircraft.Address] = e.Aircraft;
            }

            // Push the aircraft and its position to the SQL writer, if enabled
            if (_writer != null)
            {
                _logger.LogMessage(Severity.Debug, $"Queueing aircraft {e.Aircraft.Address} {e.Aircraft.Behaviour} for writing");
                _writer.Push(e.Aircraft);

                if (e.Position != null)
                {
                    _logger.LogMessage(Severity.Debug, $"Queueing position with ID {e.Position.Id} for aircraft {e.Aircraft.Address} {e.Aircraft.Behaviour} for writing");
                    _writer.Push(e.Position);
                }
            }

            // Forward the event to subscribers
            AircraftUpdated?.Invoke(this, e);
        }

        /// <summary>
        /// Handle the event raised when an existing aircraft is removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAircraftRemoved(object sender, AircraftNotificationEventArgs e)
        {
            // Remove the aircraft from the collection
            TrackedAircraft.Remove(e.Aircraft.Address, out TrackedAircraft dummy);

            // Forward the event to subscribers
            AircraftRemoved?.Invoke(this, e);
        }

        /// <summary>
        /// Handle the event raised when a batch of aircraft updates are written to the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBatchWritten(object sender, BatchWrittenEventArgs e)
            => _logger!.LogMessage(Severity.Info, $"Aircraft batch written to the database. Queue size {e.InitialQueueSize} -> {e.FinalQueueSize} in {e.Duration} ms");
    }
}
