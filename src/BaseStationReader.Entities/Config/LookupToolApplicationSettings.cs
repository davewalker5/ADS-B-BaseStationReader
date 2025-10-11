using BaseStationReader.Entities.Logging;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class LookupToolApplicationSettings : ExternalApiSettings
    {
        public Severity MinimumLogLevel { get; set; }
        public string LogFile { get; set; } = "";
        public bool CreateSightings { get; set; } = false;
        public string LiveApi { get; set; } = nameof(ApiServiceType.None);
        public string HistoricalApi { get; set; } = nameof(ApiServiceType.None);
        public string WeatherApi { get; set; } = nameof(ApiServiceType.None);
        public double? ReceiverLatitude { get; set; }
        public double? ReceiverLongitude { get; set; }
        public string ScheduleStartTime { get; set; }
        public string ScheduleEndTime { get; set; }
    }
}