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
using BaseStationReader.Interfaces.Api;
using BaseStationReader.BusinessLogic.Events;
using BaseStationReader.Interfaces.Geometry;
using BaseStationReader.Interfaces.Events;

namespace BaseStationReader.BusinessLogic.Tracking
{
    public class TrackerController : ITrackerController
    {
        private readonly IControllerNotificationSender _sender;
        private readonly IDatabaseManagementFactory _factory;
        private readonly TrackerApplicationSettings _settings;
        private IAircraftTracker _tracker = null;
        private IContinuousWriter _writer = null;

        public event EventHandler<AircraftNotificationEventArgs> AircraftAdded;
        public event EventHandler<AircraftNotificationEventArgs> AircraftUpdated;
        public event EventHandler<AircraftNotificationEventArgs> AircraftRemoved;

        private ConcurrentDictionary<string, TrackedAircraft> _trackedAircraft = new();

        public TrackerController(
            ITrackerLogger logger,
            BaseStationReaderDbContext context,
            IExternalApiFactory apiFactory,
            ITrackerHttpClient httpClient,
            ITrackerTcpClient tcpClient,
            TrackerApplicationSettings settings,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            _settings = settings;

            // Configure the database management classes
            _factory = new DatabaseManagementFactory(logger, context, _settings.TimeToLock, _settings.MaximumLookups);

            // Load the current exclusions
            var excludedAddresses = Task.Run(() => _factory.ExcludedAddressManager.ListAsync(x => true))
                .Result
                .Select(x => x.Address)
                .ToList();

            var excludedCallsigns = Task.Run(() => _factory.ExcludedCallsignManager.ListAsync(x => true))
                .Result
                .Select(x => x.Callsign)
                .ToList();

            // Configure the message reader and message parser
            var readerSender = new MessageReaderNotificationSender(logger);
            var reader = new MessageReader(tcpClient, logger, readerSender, _settings.Host, _settings.Port, _settings.SocketReadTimeout);
            var parsers = new Dictionary<MessageType, IMessageParser>
            {
                { MessageType.MSG, new MsgMessageParser() }
            };

            // Create a distance calculator
            IDistanceCalculator distanceCalculator = null;
            if ((_settings.ReceiverLatitude != null) && (_settings.ReceiverLongitude != null))
            {
                distanceCalculator = new HaversineCalculator
                {
                    ReferenceLatitude = _settings.ReceiverLatitude ?? 0,
                    ReferenceLongitude = _settings.ReceiverLongitude ?? 0
                };
            }

            // Configure the SQL writer, if enabled
            if (_settings.EnableSqlWriter)
            {
                // If auto lookup is enabled, configure the external API wrapper
                IExternalApiWrapper apiWrapper = null;
                if (_settings.AutoLookup)
                {
                    var serviceType = apiFactory.GetServiceTypeFromString(_settings.FlightApi);
                    apiWrapper = apiFactory.GetWrapperInstance(httpClient, _factory, serviceType, _settings);
                }

                _writer = new ContinuousWriter(_factory, apiWrapper, departureAirportCodes, arrivalAirportCodes, true);
            }

            // Set up the aircraft tracked helpers
            var assessor = new SimpleAircraftBehaviourAssessor();
            var propertyUpdater = new AircraftPropertyUpdater(logger, distanceCalculator, assessor);
            var trackerSender = new AircraftNotificationSender(
                logger,
                _settings.TrackedBehaviours,
                _settings.MaximumTrackedDistance,
                _settings.MinimumTrackedAltitude,
                _settings.MaximumTrackedAltitude,
                _settings.TrackPosition);

            // Construct the aircraft tracker
            _tracker = new AircraftTracker(
                reader,
                parsers,
                propertyUpdater,
                trackerSender,
                excludedAddresses,
                excludedCallsigns,
                _settings.TimeToRecent,
                _settings.TimeToStale,
                _settings.TimeToRemoval);

            // Create the controller notification sender
            _sender = new ControllerNotificationSender(logger);
        }

        /// <summary>
        /// Start tracking aircraft
        /// </summary>
        public async Task StartAsync(CancellationToken token)
        {
            // If the queued writer is enabled and clear-down is configured, clear down previous
            // tracking data
            if ((_writer != null) && _settings.ClearDown)
            {
                await _factory.Context<BaseStationReaderDbContext>()?.ClearDown();
            }

            // Attach the aircraft tracking event handlers
            _tracker.AircraftAdded += OnAircraftAdded;
            _tracker.AircraftUpdated += OnAircraftUpdated;
            _tracker.AircraftRemoved += OnAircraftRemoved;

            // Start the queued writer
            if (_writer != null)
            {
                await _writer.StartAsync(token);
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
                // Stop the continuous writer
                await _writer.StopAsync();
                await _writer.DisposeAsync();

                // Detach the aircraft tracking event handlers
                _tracker.AircraftAdded -= OnAircraftAdded;
                _tracker.AircraftUpdated -= OnAircraftUpdated;
                _tracker.AircraftRemoved -= OnAircraftRemoved;
            }
        }

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
        /// Handle the event raised when a new aircraft is detected
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAircraftAdded(object sender, AircraftNotificationEventArgs e)
        {
            HandleAircraftEvent(e.Aircraft, e.Position);
            _sender.SendAddedNotification(e.Aircraft, e.Position, this, AircraftAdded);
        }

        /// <summary>
        /// Handle the event raised when an existing aircraft is updated
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAircraftUpdated(object sender, AircraftNotificationEventArgs e)
        {
            HandleAircraftEvent(e.Aircraft, e.Position);
            _sender.SendUpdatedNotification(e.Aircraft, e.Position, this, AircraftUpdated);
        }
        /// <summary>
        /// Handle the event raised when an existing aircraft is removed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnAircraftRemoved(object sender, AircraftNotificationEventArgs e)
        {
            _trackedAircraft.Remove(e.Aircraft.Address, out TrackedAircraft _);
            _sender.SendRemovedNotification(e.Aircraft, e.Position, this, AircraftRemoved);
        }

        /// <summary>
        /// Handle the event raised when a batch of queued updates are about to be processed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBatchStarted(object sender, BatchStartedEventArgs e)
            => _factory.Logger.LogMessage(Severity.Info, $"Request batch of up to {_settings.WriterBatchSize} entries is about to be processed. Queue size {e.QueueSize}");

        /// <summary>
        /// Handle the event raised when a batch of queued updates have been processed
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnBatchCompleted(object sender, BatchCompletedEventArgs e)
            => _factory.Logger.LogMessage(Severity.Info, $"Request batch has been processed. Queue size {e.InitialQueueSize} -> {e.FinalQueueSize} in {e.Duration} ms");

        /// <summary>
        /// Handle an aircraft addition or removal event
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="position"></param>
        private void HandleAircraftEvent(TrackedAircraft aircraft, AircraftPosition position)
        {
            // If the aircraft isn't already in the collection, add it. Otherwise, update its entry
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
                // Push the aircraft to the queued writer queue
                _factory.Logger.LogMessage(Severity.Verbose, $"Queueing aircraft {aircraft.Address} {aircraft.Behaviour} for writing");
                _writer.Push(aircraft);

                // If this is a new aircraft, push a lookup request to the queued writer queue
                if (!existingAircraft && _settings.AutoLookup)
                {
                    _factory.Logger.LogMessage(Severity.Verbose, $"Queueing API lookup request for aircraft {aircraft.Address} {aircraft.Behaviour}");
                    _writer.Push(new ApiLookupRequest() { AircraftAddress = aircraft.Address });
                }

                // Push the aircraft position to the queued writer queue
                if (position != null)
                {
                    _factory.Logger.LogMessage(Severity.Verbose, $"Queueing position with ID {position.Id} for aircraft {aircraft.Address} {aircraft.Behaviour} for writing");
                    _writer.Push(position);
                }
            }
        }
    }
}
