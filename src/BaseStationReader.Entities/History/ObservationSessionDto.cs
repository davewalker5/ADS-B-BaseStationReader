#nullable enable

namespace BaseStationReader.Entities.History;

/// <summary>
/// Exposes the non-key columns of one persisted observation session.
/// </summary>
public sealed class ObservationSessionDto
{
    /// <summary>Internal key used for actions; it is never rendered as a table field.</summary>
    public int SessionId { get; set; }
    public string Name { get; set; } = string.Empty;
    public DateTime StartedAtUtc { get; set; }
    public string ProfileName { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; }
    public double? ReceiverLatitude { get; set; }
    public double? ReceiverLongitude { get; set; }
    public int? ReceiverElevation { get; set; }
    public int? MinimumAltitude { get; set; }
    public int? MaximumAltitude { get; set; }
    public int? MaximumDistance { get; set; }
    public string IncludedBehaviours { get; set; } = string.Empty;
}
