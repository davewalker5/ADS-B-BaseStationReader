using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Tracking;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Tests.Entities
{
    [ExcludeFromCodeCoverage]
    internal class AircraftNotificationData
    {
        public TrackedAircraft Aircraft { get; set; }
        public AircraftNotificationType NotificationType { get; set; }
    }
}
