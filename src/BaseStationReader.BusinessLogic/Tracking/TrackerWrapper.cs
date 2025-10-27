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
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Messages;
using BaseStationReader.Interfaces.Api;

namespace BaseStationReader.BusinessLogic.Tracking
{
    [ExcludeFromCodeCoverage]
    public class TrackerWrapper : ITrackerWrapper
    {
        private readonly ITrackerLogger _logger;
        private readonly IExternalApiFactory _apiFactory;
        private readonly ITrackerHttpClient _client;
        private readonly TrackerApplicationSettings _settings;
        private readonly IEnumerable<string> _departureAirportCodes;
        private readonly IEnumerable<string> _arrivalAirportCodes;
        private IAircraftTracker _tracker = null;
        private IQueuedWriter _writer = null;

        public event EventHandler<AircraftNotificationEventArgs> AircraftAdded;
        public event EventHandler<AircraftNotificationEventArgs> AircraftUpdated;
        public event EventHandler<AircraftNotificationEventArgs> AircraftRemoved;

        public ConcurrentDictionary<string, TrackedAircraft> TrackedAircraft { get; private set; } = new();
        public bool IsTracking { get { return (_tracker != null) && _tracker.IsTracking; } }

        public TrackerWrapper(
            ITrackerLogger logger,
            IExternalApiFactory apiFactory,
            ITrackerHttpClient client,
            TrackerApplicationSettings settings,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            _logger = logger;
            _apiFactory = apiFactory;
            _client = client;
            _settings = settings;
            _departureAirportCodes = departureAirportCodes;
            _arrivalAirportCodes = arrivalAirportCodes;
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

            // Configure the database context and management classes
            var context = new BaseStationReaderDbContextFactory().CreateDbContext(Array.Empty<string>());
            var factory = new DatabaseManagementFactory(_logger, context, _settings.TimeToLock, _settings.MaximumLookups);

            // Load the current exclusions
            var excludedAddresses = (await factory.ExcludedAddressManager.ListAsync(x => true)).Select(x => x.Address).ToList();
            var excludedCallsigns = (await factory.ExcludedCallsignManager.ListAsync(x => true)).Select(x => x.Callsign).ToList();

            // Set up the aircraft tracker
            var trackerTimer = new TrackerTimer(_settings.TimeToRecent / 10.0);
            var assessor = new SimpleAircraftBehaviourAssessor();
            var distanceCalculator = CreateDistanceCalculator();
            var propertyUpdater = new AircraftPropertyUpdater(_logger, distanceCalculator, assessor);

            var notificationSender = new NotificationSender(
                _logger,
                _settings.TrackedBehaviours,
                _settings.MaximumTrackedDistance,
                _settings.MinimumTrackedAltitude,
                _settings.MaximumTrackedAltitude,
                _settings.TrackPosition);

            _tracker = new AircraftTracker(
                reader,
                parsers,
                trackerTimer,
                propertyUpdater,
                notificationSender,
                excludedAddresses,
                excludedCallsigns,
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
                await ConfigureSqlWriter(factory);
            }
        }

        /// <summary>
        /// Start reading messages
        /// </summary>
        public void Start()
            => _tracker.Start();

        /// <summary>
        /// Stop reading messages
        /// </summary>
        public void Stop()
            => _tracker.Stop();

        /// <summary>
        /// Return the number of pending requests in the writer queue
        /// </summary>
        public int QueueSize => _writer.QueueSize;

        /// <summary>
        /// Process all pending entries in the queued writer queue
        /// </summary>
        /// <returns></returns>
        public async Task FlushQueueAsync()
            => await _writer.FlushQueueAsync();

        /// <summary>
        /// Clear all pending entries from the queued writer queue
        /// </summary>
        /// <returns></returns>
        public void ClearQueue()
            => _writer.ClearQueue();

        /// <summary>
        /// Create an instance of the distance calculator, if the receiver co-ordinates have been specified
        /// </summary>
        /// <returns></returns>
        private HaversineCalculator CreateDistanceCalculator()
            => ((_settings.ReceiverLatitude != null) && (_settings.ReceiverLongitude != null)) ?
                new HaversineCalculator
                {
                    ReferenceLatitude = _settings.ReceiverLatitude ?? 0,
                    ReferenceLongitude = _settings.ReceiverLongitude ?? 0
                } : null;

        /// <summary>
        /// Configure the SQL writer and queue processir
        /// </summary>
        /// <param name="factory"></param>
        /// <returns></returns>
        private async Task ConfigureSqlWriter(IDatabaseManagementFactory factory)
        {
            // Configure the external API wrapper
            var serviceType = _apiFactory.GetServiceTypeFromString(_settings.LiveApi);
            var apiWrapper = _apiFactory.GetWrapperInstance(
                _client,
                factory,
                serviceType,
                ApiEndpointType.Flights,
                _settings);

            // Configure the queued writer
            var writerTimer = new TrackerTimer(_settings.WriterInterval);
            _writer = new QueuedWriter(
                factory,
                apiWrapper,
                writerTimer,
                _departureAirportCodes,
                _arrivalAirportCodes,
                _settings.WriterBatchSize,
                true);
            _writer.BatchStarted += OnBatchStarted;
            _writer.BatchCompleted += OnBatchCompleted;

            // If instructed, clear down aircraft tracking data while leaving aircraft details and airlines intact
            if (_settings.ClearDown)
            {
                await factory.Context<BaseStationReaderDbContext>()?.ClearDown();
            }

            await _writer.StartAsync();
        }

        /// <summary>
        /// Handle the event raised when a new aircraft is detected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAircraftAdded(object sender, AircraftNotificationEventArgs e)
        {
            HandleAircraftEvent(e.Aircraft, e.Position);
            AircraftAdded?.Invoke(this, e);
        }

        /// <summary>
        /// Handle the event raised when an existing aircraft is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAircraftUpdated(object sender, AircraftNotificationEventArgs e)
        {
            HandleAircraftEvent(e.Aircraft, e.Position);
            AircraftUpdated?.Invoke(this, e);
        }
        /// <summary>
        /// Handle the event raised when an existing aircraft is removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAircraftRemoved(object sender, AircraftNotificationEventArgs e)
        {
            TrackedAircraft.Remove(e.Aircraft.Address, out TrackedAircraft _);
            AircraftRemoved?.Invoke(this, e);
        }

        /// <summary>
        /// Handle the event raised when a batch of queued updates are about to be processed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBatchStarted(object sender, BatchStartedEventArgs e)
            => _logger.LogMessage(Severity.Info, $"Request batch of up to {_settings.WriterBatchSize} entries is about to be processed. Queue size {e.QueueSize}");

        /// <summary>
        /// Handle the event raised when a batch of queued updates have been processed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBatchCompleted(object sender, BatchCompletedEventArgs e)
            => _logger.LogMessage(Severity.Info, $"Request batch has been processed. Queue size {e.InitialQueueSize} -> {e.FinalQueueSize} in {e.Duration} ms");

        /// <summary>
        /// Handle an aircraft addition or removal event
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="position"></param>
        private void HandleAircraftEvent(TrackedAircraft aircraft, AircraftPosition position)
        {
            // If the aircraft isn't already in the collection, add it. Otherwise, update its entry
            var existingAircraft = TrackedAircraft.ContainsKey(aircraft.Address);
            if (!existingAircraft)
            {
                TrackedAircraft[aircraft.Address] = (TrackedAircraft)aircraft.Clone();
            }
            else
            {
                TrackedAircraft[aircraft.Address] = aircraft;
            }

            // Push the aircraft and its position to the SQL writer, if enabled
            if (_writer != null)
            {
                // Push the aircraft to the queued writer queue
                _logger.LogMessage(Severity.Verbose, $"Queueing aircraft {aircraft.Address} {aircraft.Behaviour} for writing");
                _writer.Push(aircraft);

                // If this is a new aircraft, push a lookup request to the queued writer queue
                if (!existingAircraft && _settings.AutoLookup)
                {
                    _logger.LogMessage(Severity.Verbose, $"Queueing API lookup request for aircraft {aircraft.Address} {aircraft.Behaviour}");
                    _writer.Push(new ApiLookupRequest() { AircraftAddress = aircraft.Address });
                }

                // Push the aircraft position to the queued writer queue
                if (position != null)
                {
                    _logger.LogMessage(Severity.Verbose, $"Queueing position with ID {position.Id} for aircraft {aircraft.Address} {aircraft.Behaviour} for writing");
                    _writer.Push(position);
                }
            }
        }
    }
}
