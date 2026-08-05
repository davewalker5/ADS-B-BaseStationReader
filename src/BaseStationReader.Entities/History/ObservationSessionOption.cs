namespace BaseStationReader.Entities.History;

/// <summary>
/// Identifies an observation session available to the historical database browser.
/// </summary>
public sealed class ObservationSessionOption
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string ProfileName { get; init; } = string.Empty;
    public DateTime StartedAtUtc { get; init; }
}
