namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Configures the integrated UI historical database browser.
/// </summary>
public sealed class DatabaseBrowserOptions
{
    public int SessionHistoryDays { get; set; } = 7;
}
