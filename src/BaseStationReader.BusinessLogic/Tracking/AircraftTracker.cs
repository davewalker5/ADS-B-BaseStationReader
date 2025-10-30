using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Messages;
using System.Collections.Concurrent;

namespace BaseStationReader.BusinessLogic.Tracking
{
    public class AircraftTracker : IAircraftTracker
    {
        private readonly IMessageReader _reader;
        private readonly Dictionary<MessageType, IMessageParser> _parsers;
        private readonly ITrackerTimer _timer;
        private readonly IAircraftPropertyUpdater _updater;
        private readonly IAircraftNotificationSender _sender;
        private readonly IList<string> _excludedAddresses;
        private readonly IList<string> _excludedCallsigns;
        private readonly ConcurrentDictionary<string, Lazy<TrackedAircraft>> _aircraft = [];
        private CancellationTokenSource _cancellationTokenSource = null;
        private readonly int _recentMs;
        private readonly int _staleMs;
        private readonly int _removedMs;

        public event EventHandler<AircraftNotificationEventArgs> AircraftAdded;
        public event EventHandler<AircraftNotificationEventArgs> AircraftUpdated;
        public event EventHandler<AircraftNotificationEventArgs> AircraftRemoved;

        public bool IsTracking { get; private set; } = false;

        public AircraftTracker(
            IMessageReader reader,
            Dictionary<MessageType, IMessageParser> parsers,
            ITrackerTimer timer,
            IAircraftPropertyUpdater updater,
            IAircraftNotificationSender sender,
            IList<string> excludedAddresses,
            IList<string> excludedCallsigns,
            int recentMilliseconds,
            int staleMilliseconds,
            int removedMilliseconds)
        {
            _reader = reader;
            _parsers = parsers;
            _timer = timer;
            _updater = updater;
            _sender = sender;
            _excludedAddresses = excludedAddresses;
            _excludedCallsigns = excludedCallsigns;
            _timer.Tick += OnTimer;
            _recentMs = recentMilliseconds;
            _staleMs = staleMilliseconds;
            _removedMs = removedMilliseconds;
        }

        /// <summary>
        /// Start reading messages
        /// </summary>
        public void Start()
        {
            // Set the tracking flag
            IsTracking = true;

            // Start the message reader
            _cancellationTokenSource = new CancellationTokenSource();
            _reader.MessageRead += OnNewMessage;
            _reader.StartAsync(_cancellationTokenSource.Token);

            // Set a timer to migrate aircraft from New -> Recent -> Stale -> Removed
            _timer?.Start();
        }

        /// <summary>
        /// Stop reading messages
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _timer.Stop();
            IsTracking = false;
        }

        /// <summary>
        /// Event handler fired when a new message is received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNewMessage(object sender, MessageReadEventArgs e)
        {
            // Split the message into individual fields and check we have a valid message type
            var fields = e.Message.Split(",");
            if (fields.Length > 1 && Enum.TryParse(fields[0], true, out MessageType messageType) && _parsers.TryGetValue(messageType, out IMessageParser parser))
            {
                // Parse the message and check the aircraft identifier is valid and isn't excluded
                Message msg = parser.Parse(fields);
                if ((msg.Address.Length == 0) || _excludedAddresses.Contains(msg.Address))
                {
                    // Missing or excluded address
                    return;
                }

                // See if the callsign is excluded. If it is, add this aircraft as a temporary exclusion for the
                // remainder of this session. No further messages will be forwarded for this aircraft and tracking
                // details won't be written to the database
                if (!string.IsNullOrEmpty(msg.Callsign) && _excludedCallsigns.Contains(msg.Callsign))
                {
                    lock (_excludedAddresses)
                    {
                        // Don't assume the address isn't present - check first
                        if (!_excludedAddresses.Contains(msg.Address))
                        {
                            _excludedAddresses.Add(msg.Address);
                        }
                    }
                    return;
                }

                // See if this is an existing aircraft or not and either update it or add it to the tracking collection
                if (_aircraft.ContainsKey(msg.Address))
                {
                    UpdateExistingAircraft(msg);
                }
                else
                {
                    AddNewAircraft(msg);
                }
            }
        }

        /// <summary>
        /// Handle a message that updates an existing aircraft
        /// </summary>
        /// <param name="msg"></param>
        private void UpdateExistingAircraft(Message msg)
        {
            // Retrieve the existing aircraft
            var aircraft = _aircraft[msg.Address].Value;
            lock (aircraft)
            {
                // Capture the previous position
                var previousLatitude = aircraft.Latitude;
                var previousLongitude = aircraft.Longitude;
                var previousAltitude = aircraft.Altitude;
                var previousDistance = aircraft.Distance;

                // Update the aircraft propertes
                _updater.UpdateProperties(aircraft, msg);

                // Assess the aircraft behaviour
                _updater.UpdateBehaviour(aircraft, previousAltitude);

                // Send a notification to subscribers
                _sender.SendUpdatedNotification(
                    aircraft,
                    this,
                    AircraftUpdated,
                    previousLatitude,
                    previousLongitude,
                    previousAltitude,
                    previousDistance);
            }
        }

        /// <summary>
        /// Handle a message that is from a new aircraft to be added to the collection
        /// </summary>
        /// <param name="msg"></param>
        private void AddNewAircraft(Message msg)
        {
            // Add a lazily-instantiated version of the tracked aircraft with minimal properties set
            var newLazyAircraft = new Lazy<TrackedAircraft>(() => new()
            {
                Address = msg.Address,
                FirstSeen = DateTime.UtcNow
            }, isThreadSafe: true);

            // Now add it to the collection OR return the existing instance forthe specified 24-bit ICAO address
            var lazyAircraft = _aircraft.GetOrAdd(msg.Address, _ => newLazyAircraft);

            // Now see if the retrieving it returns the same instance - if it does, then the aircraft wasn't
            // there before and is newly added
            var existingLazyAircraft = _aircraft.GetOrAdd(msg.Address, lazyAircraft);
            bool isNew = ReferenceEquals(existingLazyAircraft, lazyAircraft);

            if (isNew)
            {
                // If it's new, send the "added" notification to subscribers
                _sender.SendAddedNotification(existingLazyAircraft.Value, this, AircraftAdded);
            }
        }

        /// <summary>
        /// When the timer fires, remove stale aircraft from the monitored collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimer(object sender, EventArgs e)
        {
            lock (_aircraft)
            {
                foreach (var entry in _aircraft)
                {
                    // Determine how long it is since this aircraft updated
                    var aircraft = entry.Value.Value;
                    var elapsed = (int)(DateTime.Now - aircraft.LastSeen).TotalMilliseconds;

                    // If it's now stale, remove it. Otherwise, set the staleness level and send an update
                    if (elapsed >= _removedMs)
                    {
                        _aircraft.Remove(aircraft.Address, out _);
                        _sender.SendRemovedNotification(aircraft, this, AircraftRemoved);
                    }
                    else if (elapsed >= _staleMs)
                    {
                        aircraft.Status = TrackingStatus.Stale;
                        _sender.SendStaleNotification(aircraft, this, AircraftUpdated);
                    }
                    else if (elapsed >= _recentMs)
                    {
                        aircraft.Status = TrackingStatus.Inactive;
                        _sender.SendInactiveNotification(aircraft, this, AircraftUpdated);
                    }
                }
            }
        }
    }
}