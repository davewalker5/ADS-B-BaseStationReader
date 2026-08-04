namespace BaseStationReader.Entities.History;

/// <summary>
/// Contains renderer-neutral position data and summary values for one tracked flight.
/// </summary>
public sealed class FlightProfile
{
    public int TrackingRecordId { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Callsign { get; init; } = string.Empty;
    public IReadOnlyList<FlightProfilePoint> Points { get; init; } = [];
    public DateTime? FirstTimestamp { get; init; }
    public DateTime? FinalTimestamp { get; init; }
    public decimal? InitialAltitude { get; init; }
    public decimal? FinalAltitude { get; init; }
    public decimal? MinimumAltitude { get; init; }
    public decimal? MaximumAltitude { get; init; }
    public double? ClosestDistance { get; init; }
    public double? FurthestDistance { get; init; }
    public TimeSpan Duration => FirstTimestamp.HasValue && FinalTimestamp.HasValue
        ? FinalTimestamp.Value - FirstTimestamp.Value
        : TimeSpan.Zero;
}
