namespace BaseStationReader.Entities.History;

/// <summary>
/// Represents a single position at the boundary of a tracking session.
/// </summary>
public sealed class PositionSummaryDto
{
    public DateTime Timestamp { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public decimal? Altitude { get; init; }
    public double? Distance { get; init; }
}
