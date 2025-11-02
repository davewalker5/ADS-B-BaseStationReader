using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Events
{
    public interface IAircraftNotificationSender
    {
        void SendAircraftNotification(
            TrackedAircraft aircraft,
            AircraftPosition previousPosition,
            object sender,
            AircraftNotificationType type,
            EventHandler<AircraftNotificationEventArgs> handler);
    }
}