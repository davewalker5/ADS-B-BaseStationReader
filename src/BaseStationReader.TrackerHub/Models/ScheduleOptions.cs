namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Configures the default time range shown by the schedule page.
/// </summary>
public sealed class ScheduleOptions
{
    public string ScheduleStartTime { get; set; } = "09:00";
    public string ScheduleEndTime { get; set; } = "21:00";
}
