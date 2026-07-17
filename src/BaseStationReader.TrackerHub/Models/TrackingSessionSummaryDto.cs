using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Represents one tracking record in the historical result list.
/// </summary>
public sealed class TrackingSessionSummaryDto
{
    public int Id { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Callsign { get; init; } = string.Empty;
    public string Registration { get; init; } = string.Empty;
    public DateTime FirstSeen { get; init; }
    public DateTime LastSeen { get; init; }
    public decimal? InitialAltitude { get; init; }
    public decimal? FinalAltitude { get; init; }
    public decimal? MinimumAltitude { get; init; }
    public decimal? MaximumAltitude { get; init; }
    public double? MinimumDistance { get; init; }
    public double? MaximumDistance { get; init; }
    public int PositionCount { get; init; }
    public TrackingStatus Status { get; init; }
    public TimeSpan Duration => LastSeen - FirstSeen;
}
