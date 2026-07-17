using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Represents one aircraft projected onto the receiver-centred radar plane.
/// </summary>
public sealed class RadarPointDto
{
    public string Address { get; init; } = string.Empty;
    public string Label { get; init; } = string.Empty;
    public double Distance { get; init; }
    public double Bearing { get; init; }
    public double X { get; init; }
    public double Y { get; init; }
    public decimal? Altitude { get; init; }
    public decimal? Track { get; init; }
    public TrackingStatus Status { get; init; }
    public DateTime Timestamp { get; init; }
}
