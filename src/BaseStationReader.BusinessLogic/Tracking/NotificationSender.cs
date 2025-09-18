using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Interfaces;
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
            Aircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler)
        {
            if (NotificationRequired(aircraft))
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
        public void SendUpdatedNotification(
            Aircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler,
            decimal? previousLatitude,
            decimal? previousLongitude)
        {
            if (NotificationRequired(aircraft))
            {
                // If the position's changed, create a position object to attach to the event arguments
                AircraftPosition position = null;
                if ((aircraft.Latitude != previousLatitude) || (aircraft.Longitude != previousLongitude))
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
            Aircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler)
        {
            // Messages regarding staleness and removal are sent irrespective of the qualifying criteria
            // to catch cases where the aircraft *was* tracked but now no longer qualifies
            SendNotification(aircraft, null, sender, handler, AircraftNotificationType.Stale);
        }

        /// <summary>
        /// Send an "inactive aircraft" notification to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="sender"></param>
        /// <param name="handler"></param>
        public void SendInactiveNotification(
            Aircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler)
        {
            // Messages regarding staleness and removal are sent irrespective of the qualifying criteria
            // to catch cases where the aircraft *was* tracked but now no longer qualifies
            SendNotification(aircraft, null, sender, handler, AircraftNotificationType.Recent);
        }

        /// <summary>
        /// Send an "aircraft removed" notification to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="sender"></param>
        /// <param name="handler"></param>
        public void SendRemovedNotification(
            Aircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler)
        {
            // Messages regarding staleness and removal are sent irrespective of the qualifying criteria
            // to catch cases where the aircraft *was* tracked but now no longer qualifies
            SendNotification(aircraft, null, sender, handler, AircraftNotificationType.Removed);
        }

        /// <summary>
        /// Return true if an aircraft meets the criteria for notifications to be sent
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private bool NotificationRequired(Aircraft aircraft)
            => _behaviours.Contains(aircraft.Behaviour) &&
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
            Aircraft aircraft,
            AircraftPosition position,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler,
            AircraftNotificationType type)
        {
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
        private static AircraftPosition CreateAircraftPosition(Aircraft aircraft)
        {
            AircraftPosition position = null;

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
    }
}