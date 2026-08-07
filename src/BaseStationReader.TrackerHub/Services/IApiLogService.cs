using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.History;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Provides API log operations to the Tracker Hub UI.
/// </summary>
public interface IApiLogService
{
    /// <summary>
    /// Searches the API log.
    /// </summary>
    /// <param name="filter">The search and paging criteria.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching page of entries.</returns>
    Task<PagedResult<ApiLogEntry>> SearchAsync(
        ApiLogFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes every API log entry.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The number of deleted entries.</returns>
    Task<int> ClearAsync(CancellationToken cancellationToken = default);
}
