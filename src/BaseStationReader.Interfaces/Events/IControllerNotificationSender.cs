using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Events
{
    public interface IControllerNotificationSender
    {
        void SendAircraftNotification(
            TrackedAircraft aircraft,
            AircraftPosition position,
            object sender,
            AircraftNotificationType type,
            EventHandler<AircraftNotificationEventArgs> handlers);

    }
}