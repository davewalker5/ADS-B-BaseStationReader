using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class TrackerApplicationSettings
    {
        public Severity MinimumLogLevel { get; set; }
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public int SocketReadTimeout { get; set; }
        public int ApplicationTimeout { get; set; }
        public bool RestartOnTimeout { get; set; }
        public int TimeToRecent { get; set; }
        public int TimeToStale { get; set; }
        public int TimeToRemoval { get; set; }
        public int TimeToLock { get; set; }
        public string LogFile { get; set; } = "";
        public bool VerboseLogging { get; set; }
        public bool EnableSqlWriter { get; set; }
        public bool FlushWhileActive { get; set; } = true;
        public bool FlushOnStop { get; set; } = true;
        public string SpoolFolder { get; set; } = "spool";
        public int MaximumRows { get; set; }
        public double? ReceiverLatitude { get; set; }
        public double? ReceiverLongitude { get; set; }
        public int? ReceiverElevation { get; set; }
        public int? MaximumTrackedDistance { get; set;  }
        public int? MinimumTrackedAltitude { get; set; }
        public int? MaximumTrackedAltitude { get; set; }
        public bool TrackPosition { get; set; }
        public bool TrackPositionDensity { get; set; }
        public int PositionDensityInterval { get; set; }
        public int AircraftNotificationInterval { get; set; }
        public string TrackingProfile { get; set; }
        public string TrackingProfileName { get; set; }
        public string DefaultProfileName { get; set; } = "Default Location - All Traffic";
        public List<TrackerColumn> Columns { get; set; } = [];
        public List<AircraftBehaviour> TrackedBehaviours { get; set; } = [];
    }
}
