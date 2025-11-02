using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Events
{
    [ExcludeFromCodeCoverage]
    public class AircraftNotificationEventArgs : EventArgs
    {
        public TrackedAircraft Aircraft { get; set; }
        public AircraftPosition Position { get; set; }
        public AircraftNotificationType NotificationType { get; set; }
    }
}