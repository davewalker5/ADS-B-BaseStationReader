#nullable enable

using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Provides bounded, read-only access to historical tracking sessions.
/// </summary>
public interface ITrackingSessionQueryService
{
    /// <summary>
    /// Searches historical tracking sessions using validated filters and pagination.
    /// </summary>
    /// <param name="filter">The requested search and page criteria.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>One page of tracking-session summaries.</returns>
    Task<PagedResult<TrackingSessionSummaryDto>> SearchAsync(
        TrackingSessionFilter filter,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete read-only detail for one tracking session.
    /// </summary>
    /// <param name="trackingRecordId">The tracking record identifier.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>The detail, or <see langword="null"/> when the record does not exist.</returns>
    Task<TrackingSessionDetailDto?> GetAsync(
        int trackingRecordId,
        CancellationToken cancellationToken = default);
}
