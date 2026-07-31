namespace BaseStationReader.Entities.History;

/// <summary>
/// Represents one chronologically ordered point in a historical flight profile.
/// </summary>
public sealed class FlightProfilePointDto
{
    public int Sequence { get; init; }
    public DateTime Timestamp { get; init; }
    public decimal? Latitude { get; init; }
    public decimal? Longitude { get; init; }
    public decimal? Altitude { get; init; }
    public double? Distance { get; init; }
    public double? Bearing { get; init; }
}
