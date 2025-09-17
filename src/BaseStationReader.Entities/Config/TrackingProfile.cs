using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Entities.Config
{
    public class TrackingProfile
    {
        public string Name { get; set; }
        public double? ReceiverLatitude { get; set; }
        public double? ReceiverLongitude { get; set; }
        public int? MaximumTrackedDistance { get; set;  }
        public int? MaximumTrackedAltitude { get; set; }
        public List<AircraftBehaviour> TrackedBehaviours { get; set; } = [];
    }
}