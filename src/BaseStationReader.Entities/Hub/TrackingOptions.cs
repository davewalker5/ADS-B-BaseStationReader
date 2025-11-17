using BaseStationReader.Entities.Config;

namespace BaseStationReader.Entities.Hub
{
    public class TrackingOptions
    {
        public string TrackingProfile { get; set; }
        public double? ReceiverLatitude { get; set; }
        public double? ReceiverLongitude { get; set; }
        public int? MaximumTrackedDistance { get; set;  }
        public int? MinimumTrackedAltitude { get; set; }
        public int? MaximumTrackedAltitude { get; set; }
        public string TrackedBehaviours { get; set; }

        public static TrackingOptions FromTrackerSettings(TrackerApplicationSettings settings)
            => new()
            {
                TrackingProfile = settings.TrackingProfile,
                ReceiverLatitude = settings.ReceiverLatitude,
                ReceiverLongitude = settings.ReceiverLongitude,
                MaximumTrackedDistance = settings.MaximumTrackedDistance,
                MinimumTrackedAltitude = settings.MinimumTrackedAltitude,
                MaximumTrackedAltitude = settings.MaximumTrackedAltitude,
                TrackedBehaviours = string.Join(", ", settings.TrackedBehaviours.Select(x => x.ToString()))
            };
    }
}