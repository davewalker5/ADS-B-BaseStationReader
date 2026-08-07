#nullable enable

namespace BaseStationReader.Entities.Api;

/// <summary>
/// Defines filters and paging for API log searches.
/// </summary>
public sealed class ApiLogFilter
{
    public string? Service { get; set; }

    public string? Endpoint { get; set; }

    public DateTime? FromDate { get; set; }

    public DateTime? ToDate { get; set; }

    public int Page { get; set; } = 1;

    public int PageSize { get; set; } = 25;
}
