using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Interfaces
{
    public interface INotificationSender
    {
        void SendAddedNotification(
            TrackedAircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler);

        void SendUpdatedNotification(
            TrackedAircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler,
            decimal? previousLatitude,
            decimal? previousLongitude);

        void SendStaleNotification(
            TrackedAircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler);

        void SendInactiveNotification(
            TrackedAircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler);

        void SendRemovedNotification(
            TrackedAircraft aircraft,
            object sender,
            EventHandler<AircraftNotificationEventArgs> handler);
    }
}