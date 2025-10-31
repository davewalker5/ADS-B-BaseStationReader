using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Events
{
    public interface IControllerNotificationSender
    {
        void SendAddedNotification(
            TrackedAircraft aircraft,
            AircraftPosition position,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handlers);

        void SendUpdatedNotification(
            TrackedAircraft aircraft,
            AircraftPosition position,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handlers);

        void SendRemovedNotification(
                    TrackedAircraft aircraft,
                    AircraftPosition position,
                    object sender,
                    EventHandler<AircraftNotificationEventArgs> handlers);
    }
}