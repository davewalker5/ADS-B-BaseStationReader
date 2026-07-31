namespace BaseStationReader.Entities.History;

/// <summary>
/// Defines the read-only observation-session browser criteria.
/// </summary>
public sealed class ObservationSessionFilter
{
    public int? SessionId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 25;
}
