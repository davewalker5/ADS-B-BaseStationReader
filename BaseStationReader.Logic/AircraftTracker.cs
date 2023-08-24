using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Entities.Events;
using System.Reflection;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Logic
{
    public class AircraftTracker : IAircraftTracker
    {
        private readonly IMessageReader _reader;
        private readonly Dictionary<MessageType, IMessageParser> _parsers;
        private System.Timers.Timer? _timer;
        private Dictionary<string, Aircraft> _aircraft = new();
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
            int recentMilliseconds,
            int staleMilliseconds,
            int removedMilliseconds)
        {
            _reader = reader;
            _parsers = parsers;
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
            _timer = new(interval: _recentMs / 2.0);
            _timer.Elapsed += (sender, e) => OnTimer();
            _timer.AutoReset = true;
            _timer.Enabled = true;
            _timer?.Start();
        }

        /// <summary>
        /// Stop reading messages
        /// </summary>
        public void Stop()
        {
            _cancellationTokenSource?.Cancel();
            _timer?.Stop();
            _timer?.Dispose();
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
            if (fields.Length > 1 && Enum.TryParse(fields[0], true, out MessageType messageType))
            {
                // Check we have a parser for this message type
                if (_parsers.ContainsKey(messageType))
                {
                    // Parse the message and check the aircraft identifier is valid
                    Message msg = _parsers[messageType].Parse(fields);
                    if (msg.Address.Length > 0)
                    {
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
        }

        /// <summary>
        /// Handle a message that updates an existing aircraft
        /// </summary>
        /// <param name="msg"></param>
        private void UpdateExistingAircraft(Message msg)
        {
            // Existing aircraft, so update its properties from the message and notify subscribers
            var aircraft = _aircraft[msg.Address];
            lock (aircraft)
            {
                bool changed = UpdateAircraftProperties(aircraft, msg);
                if (changed)
                {
                    AircraftUpdated?.Invoke(this, new AircraftNotificationEventArgs {
                        Aircraft = aircraft,
                        NotificationType = AircraftNotificationType.Updated
                    });
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

            AircraftAdded?.Invoke(this, new AircraftNotificationEventArgs {
                Aircraft = aircraft,
                NotificationType = AircraftNotificationType.Added
            });
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
                var messageProperty = _messageProperties.Where(x => x.Name == aircraftProperty.Name).FirstOrDefault();
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
        private void OnTimer()
        {
            lock (_aircraft)
            {
                foreach (var entry in _aircraft)
                {
                    // Determine how long it is since this aircraft updated
                    var aircraft = entry.Value;
                    var lastSeenSeconds = (decimal)(DateTime.Now - aircraft.LastSeen).TotalMilliseconds;

                    // If it's now stale, remove it. Otherwise, set the staleness level and send an update
                    if (lastSeenSeconds >= _removedMs)
                    {
                        _aircraft.Remove(entry.Key);
                        AircraftRemoved?.Invoke(this, new AircraftNotificationEventArgs {
                            Aircraft = aircraft,
                            NotificationType = AircraftNotificationType.Removed
                        });
                    }
                    else if (lastSeenSeconds >= _staleMs)
                    {
                        aircraft.Staleness = Staleness.Stale;
                        AircraftUpdated?.Invoke(this, new AircraftNotificationEventArgs {
                            Aircraft = aircraft,
                            NotificationType = AircraftNotificationType.Stale
                        });
                    }
                    else if (lastSeenSeconds >= _recentMs)
                    {
                        aircraft.Staleness = Staleness.Recent;
                        AircraftUpdated?.Invoke(this, new AircraftNotificationEventArgs {
                            Aircraft = aircraft,
                            NotificationType = AircraftNotificationType.Recent
                        });
                    }
                }
            }
        }
    }
}