using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Events
{
    [ExcludeFromCodeCoverage]
    public class AircraftNotificationEventArgs : EventArgs
    {
#pragma warning disable CS8618
        public Aircraft Aircraft { get; set; }
#pragma warning restore CS8618
        public AircraftPosition Position { get; set; }
        public AircraftNotificationType NotificationType { get; set; }
    }
}