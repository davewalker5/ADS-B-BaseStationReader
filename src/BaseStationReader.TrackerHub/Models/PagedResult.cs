namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Contains one bounded page of query results and its paging metadata.
/// </summary>
/// <typeparam name="T">The result item type.</typeparam>
public sealed class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; init; } = [];
    public int Page { get; init; }
    public int PageSize { get; init; }
    public int TotalCount { get; init; }
    public int TotalPages => TotalCount == 0 ? 0 : (int)Math.Ceiling(TotalCount / (double)PageSize);
}
