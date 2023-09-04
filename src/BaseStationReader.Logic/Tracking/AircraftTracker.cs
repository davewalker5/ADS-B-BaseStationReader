using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Tracking;
using System.Reflection;

namespace BaseStationReader.Logic.Tracking
{
    public class AircraftTracker : IAircraftTracker
    {
        private readonly IMessageReader _reader;
        private readonly Dictionary<MessageType, IMessageParser> _parsers;
        private readonly ITrackerLogger _logger;
        private readonly ITrackerTimer _timer;
        private readonly Dictionary<string, Aircraft> _aircraft = new();
        private CancellationTokenSource? _cancellationTokenSource = null;
        private readonly int _recentMs;
        private readonly int _staleMs;
        private readonly int _removedMs;

        private readonly PropertyInfo[] _aircraftProperties = typeof(Aircraft).GetProperties(BindingFlags.Instance | BindingFlags.Public);
        private readonly PropertyInfo[] _messageProperties = typeof(Message).GetProperties(BindingFlags.Instance | BindingFlags.Public);

        public event EventHandler<AircraftNotificationEventArgs>? AircraftAdded;
        public event EventHandler<AircraftNotificationEventArgs>? AircraftUpdated;
        public event EventHandler<AircraftNotificationEventArgs>? AircraftRemoved;

        public AircraftTracker(
            IMessageReader reader,
            Dictionary<MessageType, IMessageParser> parsers,
            ITrackerLogger logger,
            ITrackerTimer timer,
            int recentMilliseconds,
            int staleMilliseconds,
            int removedMilliseconds)
        {
            _reader = reader;
            _parsers = parsers;
            _logger = logger;
            _timer = timer;
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
            // Start the message reader
            _cancellationTokenSource = new CancellationTokenSource();
            _reader.MessageRead += OnNewMessage;
            _reader.Start(_cancellationTokenSource.Token);

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
        }

        /// <summary>
        /// Event handler fired when a new message is received
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnNewMessage(object? sender, MessageReadEventArgs e)
        {
            // Split the message into individual fields and check we have a valid message type
            var fields = e.Message.Split(",");
            if (fields.Length > 1 && Enum.TryParse(fields[0], true, out MessageType messageType) && _parsers.TryGetValue(messageType, out IMessageParser? parser))
            {
                // Parse the message and check the aircraft identifier is valid
                Message msg = parser.Parse(fields);
                if (msg.Address.Length > 0)
                {
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
        }

        /// <summary>
        /// Handle a message that updates an existing aircraft
        /// </summary>
        /// <param name="msg"></param>
        private void UpdateExistingAircraft(Message msg)
        {
            // Retrieve the existing aircraft
            var aircraft = _aircraft[msg.Address];
            lock (aircraft)
            {
                // Capture the previous position
                var lastLatitude = aircraft.Latitude;
                var lastLongitude = aircraft.Longitude;

                // Determine if it's changed and update its properties
                bool changed = UpdateAircraftProperties(aircraft, msg);
                if (changed)
                {
                    try
                    {
                        // If the position's changed, construct a position instance to add to the notification event arguments
                        AircraftPosition? position = null;
                        if (aircraft.Latitude != lastLatitude || aircraft.Longitude != lastLongitude)
                        {
                            position = CreateAircraftPosition(aircraft);
                        }

                        // Notify subscribers
                        AircraftUpdated?.Invoke(this, new AircraftNotificationEventArgs
                        {
                            Aircraft = aircraft,
                            Position = position,
                            NotificationType = AircraftNotificationType.Updated
                        });
                    }
                    catch (Exception ex)
                    {
                        // Log and sink the exception. The tracker has to be protected from errors in the
                        // subscriber callbacks or the application will stop updating
                        _logger.LogException(ex);
                    }
                }
            }
        }

        /// <summary>
        /// Handle a message that is from a new aircraft to be added to the collection
        /// </summary>
        /// <param name="msg"></param>
        private void AddNewAircraft(Message msg)
        {
            var aircraft = new Aircraft { FirstSeen = DateTime.Now };
            UpdateAircraftProperties(aircraft, msg);

            lock (_aircraft)
            {
                _aircraft.Add(msg.Address, aircraft);
            }

            try
            {
                AircraftAdded?.Invoke(this, new AircraftNotificationEventArgs
                {
                    Aircraft = aircraft,
                    Position = CreateAircraftPosition(aircraft),
                    NotificationType = AircraftNotificationType.Added
                });
            }
            catch (Exception ex)
            {
                // Log and sink the exception. The tracker has to be protected from errors in the
                // subscriber callbacks or the application will stop updating
                _logger.LogException(ex);
            }
        }

        /// <summary>
        /// Create and return aircraft position if the specified aircraft has valid latitude and longitude
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private static AircraftPosition? CreateAircraftPosition(Aircraft aircraft)
        {
            AircraftPosition? position = null;

            if (aircraft.Altitude != null && aircraft.Latitude != null && aircraft.Longitude != null)
            {
                // Note that both the address and ID of the aircraft are added to the position. The address
                // isn't persisted, but is used to map a position to an existing aircraft in cases where a
                // new aircraft is detected and its position is pushed to the queue in the same batch as the
                // aircraft itself
                position = new AircraftPosition
                {
                    Id = aircraft.Id,
                    Address = aircraft.Address,
                    Altitude = aircraft.Altitude ?? 0M,
                    Latitude = aircraft.Latitude ?? 0M,
                    Longitude = aircraft.Longitude ?? 0M,
                    Timestamp = aircraft.LastSeen
                };
            }

            return position;
        }

        /// <summary>
        /// Update an aircraft instance from a received message
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="msg"></param>
        private bool UpdateAircraftProperties(Aircraft aircraft, Message msg)
        {
            bool changed = false;

            foreach (var aircraftProperty in _aircraftProperties)
            {
                var messageProperty = Array.Find(_messageProperties, x => x.Name == aircraftProperty.Name);
                if (messageProperty != null)
                {
                    var original = aircraftProperty.GetValue(aircraft);
                    var updated = messageProperty.GetValue(msg);
                    if (updated != null && original != updated)
                    {
                        aircraftProperty.SetValue(aircraft, updated);
                        aircraft.Staleness = Staleness.New;
                        changed = true;
                    }
                }
            }

            return changed;
        }

        /// <summary>
        /// When the timer fires, remove stale aircraft from the monitored collection
        /// </summary>
        /// <param name="sender"></param>
        /// <param name="e"></param>
        private void OnTimer(object? sender, EventArgs e)
        {
            lock (_aircraft)
            {
                foreach (var entry in _aircraft)
                {
                    // Determine how long it is since this aircraft updated
                    var aircraft = entry.Value;
#pragma warning disable S6561
                    var elapsed = (int)(DateTime.Now - aircraft.LastSeen).TotalMilliseconds;
#pragma warning restore S6561

                    try
                    {
                        // If it's now stale, remove it. Otherwise, set the staleness level and send an update
                        if (elapsed >= _removedMs)
                        {
                            _aircraft.Remove(entry.Key);
                            AircraftRemoved?.Invoke(this, new AircraftNotificationEventArgs
                            {
                                Aircraft = aircraft,
                                NotificationType = AircraftNotificationType.Removed
                            });
                        }
                        else if (elapsed >= _staleMs)
                        {
                            aircraft.Staleness = Staleness.Stale;
                            AircraftUpdated?.Invoke(this, new AircraftNotificationEventArgs
                            {
                                Aircraft = aircraft,
                                NotificationType = AircraftNotificationType.Stale
                            });
                        }
                        else if (elapsed >= _recentMs)
                        {
                            aircraft.Staleness = Staleness.Recent;
                            AircraftUpdated?.Invoke(this, new AircraftNotificationEventArgs
                            {
                                Aircraft = aircraft,
                                NotificationType = AircraftNotificationType.Recent
                            });
                        }
                    }
                    catch (Exception ex)
                    {
                        // Log and sink the exception. The tracker has to be protected from errors in the
                        // subscriber callbacks or the application will stop updating
                        _logger.LogException(ex);
                    }
                }
            }
        }
    }
}