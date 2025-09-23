using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking
{
    public class NotificationSender : INotificationSender
    {
        private readonly ITrackerLogger _logger;
        private readonly int? _maximumDistance;
        private readonly int? _minimumAltitude;
        private readonly int? _maximumAltitude;
        private readonly IEnumerable<AircraftBehaviour> _behaviours;

        public NotificationSender(
            ITrackerLogger logger,
            IEnumerable<AircraftBehaviour> behaviours,
            int? maximumDistance,
            int? minimumAltitude,
            int? maximumAltitude)
        {
            _logger = logger;
            _maximumDistance = maximumDistance;
            _minimumAltitude = minimumAltitude;
            _maximumAltitude = maximumAltitude;
            _behaviours = behaviours;
        }

        /// <summary>
        /// Send an "aircraft added" notification to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="sender"></param>
        /// <param name="handler"></param>
        /// <param name="type"></param>
        /// <param name="previousLatitude"></param>
        /// <param name="previousLongitude"></param>
        public void SendAddedNotification(
            TrackedAircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler)
        {
            if (CheckTrackingCriteria(aircraft))
            {
                SendNotification(aircraft, null, sender, handler, AircraftNotificationType.Added);
            }
        }

        /// <summary>
        /// Send an "aircraft updated" notification to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="sender"></param>
        /// <param name="handler"></param>
        /// <param name="type"></param>
        /// <param name="previousLatitude"></param>
        /// <param name="previousLongitude"></param>
        /// <param name="previousAltitude"></param>
        /// <param name="previousDistance"></param>
        public void SendUpdatedNotification(
            TrackedAircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler,
            decimal? previousLatitude,
            decimal? previousLongitude,
            decimal? previousAltitude,
            double? previousDistance)
        {
            if (CheckTrackingCriteria(aircraft))
            {
                // If the position's changed, create a position object to attach to the event arguments
                AircraftPosition position = null;
                if ((aircraft.Latitude != previousLatitude) ||
                    (aircraft.Longitude != previousLongitude) ||
                    (aircraft.Altitude != previousAltitude) ||
                    (aircraft.Distance != previousDistance))
                {
                    position = CreateAircraftPosition(aircraft);
                }

                // Send the notification
                SendNotification(aircraft, position, sender, handler, AircraftNotificationType.Updated);
            }
        }

        /// <summary>
        /// Send a "stale aircraft" notification to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="sender"></param>
        /// <param name="handler"></param>
        public void SendStaleNotification(
            TrackedAircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler)
        {
            // Messages regarding staleness and removal aren't subject to the distance and altitude
            // constraints, only the aircraft behaviour constraints
            if (CheckBehaviourMatches(aircraft))
            {
                SendNotification(aircraft, null, sender, handler, AircraftNotificationType.Stale);
            }
        }

        /// <summary>
        /// Send an "inactive aircraft" notification to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="sender"></param>
        /// <param name="handler"></param>
        public void SendInactiveNotification(
            TrackedAircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler)
        {
            // Messages regarding staleness and removal aren't subject to the distance and altitude
            // constraints, only the aircraft behaviour constraints
            if (CheckBehaviourMatches(aircraft))
            {
                SendNotification(aircraft, null, sender, handler, AircraftNotificationType.Recent);
            }
        }

        /// <summary>
        /// Send an "aircraft removed" notification to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="sender"></param>
        /// <param name="handler"></param>
        public void SendRemovedNotification(
            TrackedAircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler)
        {
            // Messages regarding staleness and removal aren't subject to the distance and altitude
            // constraints, only the aircraft behaviour constraints
            if (CheckBehaviourMatches(aircraft))
            {
                SendNotification(aircraft, null, sender, handler, AircraftNotificationType.Removed);
            }
        }

        /// <summary>
        /// Return true if the behaviour of an aircraft matches the tracking criteria
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private bool CheckBehaviourMatches(TrackedAircraft aircraft)
            => _behaviours.Contains(aircraft.Behaviour);

        /// <summary>
        /// Return true if an aircraft meets the criteria for notifications to be sent
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private bool CheckTrackingCriteria(TrackedAircraft aircraft)
            => CheckBehaviourMatches(aircraft) &&
               ((_maximumDistance == null) || (aircraft.Distance <= _maximumDistance)) &&
               ((_minimumAltitude == null) || (aircraft.Altitude >= _minimumAltitude)) &&
               ((_maximumAltitude == null) || (aircraft.Altitude <= _maximumAltitude));

        /// <summary>
        /// Send a notification of the specified type to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="sender"></param>
        /// <param name="handler"></param>
        /// <param name="type"></param>
        /// <param name="previousLatitude"></param>
        /// <param name="previousLongitude"></param>
        private void SendNotification(
            TrackedAircraft aircraft,
            AircraftPosition position,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler,
            AircraftNotificationType type)
        {
            _logger.LogMessage(Severity.Debug, $"Sending {type} message for aircraft {aircraft.Address} {aircraft.Behaviour}");

            try
            {
                // Send the notification to subscribers
                handler?.Invoke(sender, new AircraftNotificationEventArgs
                {
                    Aircraft = aircraft,
                    Position = position,
                    NotificationType = type
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
        private static AircraftPosition CreateAircraftPosition(TrackedAircraft aircraft)
        {
            AircraftPosition position = null;

            if (aircraft.Altitude != null && aircraft.Latitude != null && aircraft.Longitude != null)
            {
                position = new AircraftPosition
                {
                    Address = aircraft.Address,
                    Altitude = aircraft.Altitude,
                    Latitude = aircraft.Latitude,
                    Longitude = aircraft.Longitude,
                    Distance = aircraft.Distance,
                    Timestamp = aircraft.LastSeen
                };
            }

            return position;
        }
    }
}