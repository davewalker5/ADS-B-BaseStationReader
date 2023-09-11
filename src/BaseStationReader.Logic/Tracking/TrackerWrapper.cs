using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Logic.Database;
using BaseStationReader.Logic.Messages;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Logic.Tracking
{
    [ExcludeFromCodeCoverage]
    public class TrackerWrapper : ITrackerWrapper
    {
        private readonly ITrackerLogger _logger;
        private IAircraftTracker? _tracker = null;
        private readonly ApplicationSettings _settings;
        private IQueuedWriter? _writer = null;

        public event EventHandler<AircraftNotificationEventArgs>? AircraftAdded;
        public event EventHandler<AircraftNotificationEventArgs>? AircraftUpdated;
        public event EventHandler<AircraftNotificationEventArgs>? AircraftRemoved;

        public ConcurrentDictionary<string, Aircraft> TrackedAircraft { get; private set; } = new();
        public bool IsTracking { get { return (_tracker != null) && _tracker.IsTracking; } }

        public TrackerWrapper(ITrackerLogger logger, ApplicationSettings settings)
        {
            _logger = logger;
            _settings = settings;
        }

        /// <summary>
        /// Initialise the tracking and writing system
        /// </summary>
        public void Initialise()
        {
            // Log the settings on startup
            _logger.LogMessage(Severity.Debug, $"Host = {_settings.Host}");
            _logger.LogMessage(Severity.Debug, $"Port = {_settings.Port}");
            _logger.LogMessage(Severity.Debug, $"SocketReadTimeout = {_settings.SocketReadTimeout}");
            _logger.LogMessage(Severity.Debug, $"ApplicationTimeout = {_settings.ApplicationTimeout}");
            _logger.LogMessage(Severity.Debug, $"TimeToRecent = {_settings.TimeToRecent}");
            _logger.LogMessage(Severity.Debug, $"TimeToStale = {_settings.TimeToStale}");
            _logger.LogMessage(Severity.Debug, $"TimeToRemoval = {_settings.TimeToRemoval}");
            _logger.LogMessage(Severity.Debug, $"TimeToLock = {_settings.TimeToLock}");
            _logger.LogMessage(Severity.Debug, $"LogFile = {_settings.LogFile}");
            _logger.LogMessage(Severity.Debug, $"EnableSqlWriter = {_settings.EnableSqlWriter}");
            _logger.LogMessage(Severity.Debug, $"WriterInterval = {_settings.WriterInterval}");
            _logger.LogMessage(Severity.Debug, $"WriterBatchSize = {_settings.WriterBatchSize}");

            // Set up the message reader and parser
            var reader = new MessageReader(_logger, _settings.Host, _settings.Port, _settings.SocketReadTimeout);
            var parsers = new Dictionary<MessageType, IMessageParser>
            {
                { MessageType.MSG, new MsgMessageParser() }
            };

            // Set up the aircraft tracker
            var trackerTimer = new TrackerTimer(_settings.TimeToRecent / 10.0);
            _tracker = new AircraftTracker(reader,
                parsers,
                _logger!,
                trackerTimer,
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
                var aircraftWriter = new AircraftWriter(context);
                var positionWriter = new PositionWriter(context);
                var aircraftLocker = new AircraftLockManager(aircraftWriter, _settings.TimeToLock);
                var writerTimer = new TrackerTimer(_settings.WriterInterval);
                _writer = new QueuedWriter(aircraftWriter, positionWriter, aircraftLocker, _logger!, writerTimer, _settings.WriterBatchSize);
                _writer.BatchWritten += OnBatchWritten;
                _writer.Start();
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
        private void OnAircraftAdded(object? sender, AircraftNotificationEventArgs e)
        {
            // Add the aircraft to the collection
            TrackedAircraft[e.Aircraft.Address] = (Aircraft)e.Aircraft.Clone();

            // Push the aircraft and its position to the SQL writer, if enabled
            if (_writer != null)
            {
                _logger.LogMessage(Severity.Debug, $"Queueing aircraft {e.Aircraft.Address} for writing");
                _writer.Push(e.Aircraft);

                if (e.Position != null)
                {
                    _logger.LogMessage(Severity.Debug, $"Queueing position for aircraft {e.Aircraft.Address} for writing");
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
        private void OnAircraftUpdated(object? sender, AircraftNotificationEventArgs e)
        {
            // Update the aircraft in the collection
            TrackedAircraft[e.Aircraft.Address] = e.Aircraft;

            // Push the aircraft and its position to the SQL writer, if enabled
            if (_writer != null)
            {
                _logger.LogMessage(Severity.Debug, $"Queueing aircraft {e.Aircraft.Address} for writing");
                _writer.Push(e.Aircraft);

                if (e.Position != null)
                {
                    _logger.LogMessage(Severity.Debug, $"Queueing position for aircraft {e.Aircraft.Address} for writing");
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
        private void OnAircraftRemoved(object? sender, AircraftNotificationEventArgs e)
        {
            // Remove the aircraft from the collection
            TrackedAircraft.Remove(e.Aircraft.Address, out Aircraft? dummy);

            // Forward the event to subscribers
            AircraftRemoved?.Invoke(this, e);
        }

        /// <summary>
        /// Handle the event raised when a batch of aircraft updates are written to the database
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBatchWritten(object? sender, BatchWrittenEventArgs e)
            => _logger!.LogMessage(Severity.Info, $"Aircraft batch written to the database. Queue size {e.InitialQueueSize} -> {e.FinalQueueSize} in {e.Duration} ms");
    }
}
