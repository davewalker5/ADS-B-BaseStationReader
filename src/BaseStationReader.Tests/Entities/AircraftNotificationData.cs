using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Tests.Entities
{
    internal class AircraftNotificationData
    {
        public TrackedAircraft Aircraft { get; set; }
        public AircraftNotificationType NotificationType { get; set; }
    }
}
