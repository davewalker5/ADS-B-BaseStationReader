using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class TrackingProfile
    {
        public string Name { get; set; }
        public double? ReceiverLatitude { get; set; }
        public double? ReceiverLongitude { get; set; }
        public int? ReceiverElevation { get; set; }
        public int? MaximumTrackedDistance { get; set; }
        public int? MinimumTrackedAltitude { get; set; }
        public int? MaximumTrackedAltitude { get; set; }
        public List<AircraftBehaviour> TrackedBehaviours { get; set; } = [];
    }
}