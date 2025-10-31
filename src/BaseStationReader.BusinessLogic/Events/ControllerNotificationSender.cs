
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Events;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Events
{
    public class ControllerNotificationSender : SubscriberNotifier, IControllerNotificationSender
    {
        public ControllerNotificationSender(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Send an "aircraft added" notification to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="position"></param>
        /// <param name="sender"></param>
        /// <param name="handlers"></param>
        public void SendAddedNotification(
            TrackedAircraft aircraft,
            AircraftPosition position,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handlers)
            => SendNotification(aircraft, position, sender, handlers, AircraftNotificationType.Added);

        /// <summary>
        /// Send an "aircraft updated" notification to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="position"></param>
        /// <param name="sender"></param>
        /// <param name="handlers"></param>
        public void SendUpdatedNotification(
            TrackedAircraft aircraft,
            AircraftPosition position,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handlers)
            => SendNotification(aircraft, position, sender, handlers, AircraftNotificationType.Updated);

        /// <summary>
        /// Send an "aircraft removed" notification to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="position"></param>
        /// <param name="sender"></param>
        /// <param name="handlers"></param>
        public void SendRemovedNotification(
            TrackedAircraft aircraft,
            AircraftPosition position,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handlers)
            => SendNotification(aircraft, position, sender, handlers, AircraftNotificationType.Removed);

        /// <summary>
        /// Send the notification to subscribers
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="position"></param>
        /// <param name="sender"></param>
        /// <param name="handlers"></param>
        /// <param name="type"></param>
        public void SendNotification(
            TrackedAircraft aircraft,
            AircraftPosition position,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handlers,
            AircraftNotificationType type)
        {
            // Construct the event arguments
            var eventArgs = new AircraftNotificationEventArgs
            {
                Aircraft = aircraft,
                Position = position,
                NotificationType = type
            };

            // Notify subscribers
            NotifySubscribers(sender, handlers, eventArgs);
        }
    }
}