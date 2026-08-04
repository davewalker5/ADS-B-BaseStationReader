namespace BaseStationReader.Entities.History;

/// <summary>Identifies the aircraft and value behind a session highlight.</summary>
public sealed class ObservationHighlight
{
    public string Address { get; init; } = string.Empty;
    public string Callsign { get; init; } = string.Empty;
    public decimal? Altitude { get; init; }
    public double? Distance { get; init; }
    public TimeSpan? Duration { get; init; }
}
