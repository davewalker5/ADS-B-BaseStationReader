using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Events
{
    public abstract class AircraftNotificationSenderBase : SubscriberNotifier
    {
        public AircraftNotificationSenderBase(ITrackerLogger logger) : base(logger)
        {

        }

        /// <summary>
        /// Send a notification of the specified type to the subscribers to the specified handler
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="sender"></param>
        /// <param name="handlers"></param>
        /// <param name="type"></param>
        /// <param name="previousLatitude"></param>
        /// <param name="previousLongitude"></param>
        public virtual void SendAircraftNotification(
            TrackedAircraft aircraft,
            AircraftPosition position,
            object sender,
            AircraftNotificationType type,
            EventHandler<AircraftNotificationEventArgs> handlers)
        {
            Logger.LogMessage(Severity.Verbose, $"Sending {type} message for aircraft {aircraft.Address} {aircraft.Behaviour}");

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