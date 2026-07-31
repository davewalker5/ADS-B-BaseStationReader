namespace BaseStationReader.Entities.History;

/// <summary>
/// Contains renderer-neutral identity and ordered position data for one historical tracking record.
/// </summary>
public sealed class TrackingProfileDataDto
{
    public int Id { get; init; }
    public string Address { get; init; } = string.Empty;
    public string Callsign { get; init; } = string.Empty;
    public IReadOnlyList<FlightProfilePointDto> Points { get; init; } = [];
}
