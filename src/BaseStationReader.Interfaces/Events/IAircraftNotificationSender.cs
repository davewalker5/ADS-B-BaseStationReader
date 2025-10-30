using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Events
{
    public interface IAircraftNotificationSender
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
            decimal? previousLongitude,
            decimal? previousAltitude,
            double? previousDistance);

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