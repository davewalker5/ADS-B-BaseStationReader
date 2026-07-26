#nullable enable

using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Provides bounded, read-only access to historical tracking sessions.
/// </summary>
public interface ITrackingSessionQueryService
{
    /// <summary>Returns the newest persisted observation session identifier.</summary>
    Task<int?> GetLatestSessionIdAsync(CancellationToken cancellationToken = default);

    /// <summary>Builds a read-only summary from data persisted for one observation session.</summary>
    Task<ObservationSessionSummaryDto?> GetObservationSessionSummaryAsync(
        int sessionId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Lists observation sessions that contain historical tracking records.
    /// </summary>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>Available sessions ordered from newest to oldest.</returns>
    Task<IReadOnlyList<ObservationSessionOptionDto>> ListSessionsAsync(
        CancellationToken cancellationToken = default);

    /// <summary>Searches persisted observation sessions by session start date.</summary>
    Task<PagedResult<ObservationSessionDto>> SearchObservationSessionsAsync(
        ObservationSessionFilter filter,
        CancellationToken cancellationToken = default);

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

    /// <summary>
    /// Retrieves and prepares the ordered flight profile for one tracking session.
    /// </summary>
    /// <param name="trackingRecordId">The tracking record identifier.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>The flight profile, or <see langword="null"/> when the record does not exist.</returns>
    Task<FlightProfileDto?> GetFlightProfileAsync(
        int trackingRecordId,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves and prepares the geographic and 3D flight path for one tracking session.
    /// </summary>
    /// <param name="trackingRecordId">The tracking record identifier.</param>
    /// <param name="cancellationToken">Cancels the database query.</param>
    /// <returns>The flight path, or <see langword="null"/> when the record does not exist.</returns>
    Task<FlightPathDto?> GetFlightPathAsync(
        int trackingRecordId,
        CancellationToken cancellationToken = default);
}
