using BaseStationReader.Entities.Events;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Messages;
using System.Collections.Concurrent;
using BaseStationReader.Interfaces.Events;

namespace BaseStationReader.BusinessLogic.Tracking
{
    public class AircraftTracker : IAircraftTracker
    {
        private readonly System.Timers.Timer _timer;
        private readonly object _lock = new();
        private readonly IMessageReader _reader;
        private readonly Dictionary<MessageType, IMessageParser> _parsers;
        private readonly IAircraftPropertyUpdater _updater;
        private readonly IAircraftNotificationSender _sender;
        private readonly ConcurrentBag<string> _excludedAddresses;
        private readonly ConcurrentBag<string> _excludedCallsigns;
        private readonly ConcurrentDictionary<string, TrackedAircraft> _aircraft = [];
        private readonly int _recentMs;
        private readonly int _staleMs;
        private readonly int _removedMs;

        public event EventHandler<AircraftNotificationEventArgs> AircraftEvent;

        public AircraftTracker(
            IMessageReader reader,
            Dictionary<MessageType, IMessageParser> parsers,
            IAircraftPropertyUpdater updater,
            IAircraftNotificationSender sender,
            IList<string> excludedAddresses,
            IList<string> excludedCallsigns,
            int recentMilliseconds,
            int staleMilliseconds,
            int removedMilliseconds)
        {
            // Make a valid timer interval
            var interval = Math.Max(100, recentMilliseconds / 10.0);

            // Initialise the timer
            _timer = new System.Timers.Timer(interval)
            {
                AutoReset = true,
                Enabled = false
            };

            // Hook up the method called on each "tick"
            _timer.Elapsed += OnTimer;

            // Populate the exclusions
            _excludedAddresses = [.. excludedAddresses];
            _excludedCallsigns = [.. excludedCallsigns];

            _reader = reader;
            _parsers = parsers;
            _updater = updater;
            _sender = sender;
            _recentMs = recentMilliseconds;
            _staleMs = staleMilliseconds;
            _removedMs = removedMilliseconds;
        }

        /// <summary>
        /// Start tracking aircraft
        /// </summary>
        /// <param name="token"></param>
        /// <returns></returns>
        public async Task StartAsync(CancellationToken token)
        {
            try
            {
                // Start the message reader
                _reader.MessageRead += OnNewMessage;
                _timer.Start();
                await _reader.StartAsync(token);
            }
            catch (TaskCanceledException)
            {
                // Expected when the token is cancelled
                throw;
            }
            finally
            {
                _reader.MessageRead -= OnNewMessage;
                _timer.Stop();
            }
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
            if ((fields.Length > 1) && Enum.TryParse(fields[0], true, out MessageType messageType) && _parsers.TryGetValue(messageType, out IMessageParser parser))
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
                    _excludedAddresses.Add(msg.Address);
                    return;
                }

                // Create a new tracked aircraft instance and update it from the message
                var newTrackedAircraft = new TrackedAircraft() { FirstSeen = DateTime.Now };

                // Add the new aircraft to the collection OR return the existing instance for the specified 24-bit
                // ICAO address if it's already been added
                var trackedAircraft = _aircraft.GetOrAdd(msg.Address, _ => newTrackedAircraft);
                var isNew = ReferenceEquals(trackedAircraft, newTrackedAircraft);

                // If it's not a new aircraft, capture the position before updating properties from the message.
                // Otherwise, just use the position from the message
                AircraftPosition position = isNew ? null : new()
                {
                    Address = trackedAircraft.Address,
                    Latitude = trackedAircraft.Latitude,
                    Longitude = trackedAircraft.Longitude,
                    Altitude = trackedAircraft.Altitude,
                    Distance = trackedAircraft.Distance
                };

                // Update the properties on the aircraft from the message
                _updater.UpdateProperties(trackedAircraft, msg);

                // Assess the aircraft behaviour
                _updater.UpdateBehaviour(trackedAircraft, position?.Altitude);

                // Send the notification
                var type = isNew ? AircraftNotificationType.Added : AircraftNotificationType.Updated;
                _sender.SendAircraftNotification(trackedAircraft, position, this, type, AircraftEvent);
            }
        }

        /// <summary>
        /// When the timer fires, remove stale aircraft from the monitored collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimer(object sender, EventArgs e)
        {
            foreach (var entry in _aircraft)
            {
                // Determine how long it is since this aircraft updated
                var aircraft = entry.Value;
                var elapsed = (int)(DateTime.Now - aircraft.LastSeen).TotalMilliseconds;

                // If it's now stale, remove it. Otherwise, set the staleness level and send an update
                if (elapsed >= _removedMs)
                {
                    _aircraft.Remove(aircraft.Address, out _);
                    _sender.SendAircraftNotification(aircraft, null, this, AircraftNotificationType.Removed, AircraftEvent);
                }
                else if (elapsed >= _staleMs)
                {
                    aircraft.Status = TrackingStatus.Stale;
                    _sender.SendAircraftNotification(aircraft, null, this, AircraftNotificationType.Stale, AircraftEvent);
                }
                else if (elapsed >= _recentMs)
                {
                    aircraft.Status = TrackingStatus.Inactive;
                    _sender.SendAircraftNotification(aircraft, null, this, AircraftNotificationType.Recent, AircraftEvent);
                }
            }
        }
    }
}