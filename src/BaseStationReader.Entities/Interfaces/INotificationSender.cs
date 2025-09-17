using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface INotificationSender
    {
        bool NotificationRequired(Aircraft aircraft);

        void SendAddedNotification(
            Aircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler);

        void SendUpdatedNotification(
            Aircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler,
            decimal? previousLatitude,
            decimal? previousLongitude);

        void SendStaleNotification(
            Aircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler);

        void SendInactiveNotification(
            Aircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler);

        void SendRemovedNotification(
            Aircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler);
    }
}