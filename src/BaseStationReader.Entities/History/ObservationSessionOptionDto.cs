namespace BaseStationReader.Entities.History;

/// <summary>
/// Identifies an observation session available to the historical database browser.
/// </summary>
public sealed class ObservationSessionOptionDto
{
    public int Id { get; init; }
    public string ProfileName { get; init; } = string.Empty;
    public DateTime StartedAtUtc { get; init; }
}
