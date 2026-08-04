#nullable enable

using BaseStationReader.Entities.History;

namespace BaseStationReader.Interfaces.Database;

/// <summary>
/// Provides bounded, read-only historical tracking queries from the business-logic layer.
/// </summary>
public interface ITrackingSessionQueryManager
{
    /// <summary>Returns the newest persisted observation-session identifier.</summary>
    Task<int?> GetLatestSessionIdAsync(CancellationToken cancellationToken = default);

    /// <summary>Builds a read-only aggregate for one observation session.</summary>
    Task<ObservationSessionSummary?> GetObservationSessionSummaryAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves and aggregates valid persisted positions belonging exclusively to one observation session.
    /// </summary>
    /// <param name="sessionId">The required observation-session identifier.</param>
    /// <param name="cancellationToken">Cancels the scoped query and aggregation.</param>
    /// <returns>The density model, or <see langword="null"/> when the session does not exist.</returns>
    Task<PositionDensity?> GetPositionDensityAsync(int sessionId, CancellationToken cancellationToken = default);

    /// <summary>Lists recent observation sessions within the requested history window.</summary>
    Task<IReadOnlyList<ObservationSessionOption>> ListSessionsAsync(int historyDays, CancellationToken cancellationToken = default);

    /// <summary>Searches and pages observation-session metadata.</summary>
    Task<PagedResult<ObservationSessionDto>> SearchObservationSessionsAsync(ObservationSessionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Searches and pages historical aircraft tracking records.</summary>
    Task<PagedResult<TrackingSessionSummary>> SearchAsync(TrackingSessionFilter filter, CancellationToken cancellationToken = default);

    /// <summary>Returns the enriched historical detail for one tracking record.</summary>
    Task<TrackingSessionDetail?> GetAsync(int trackingRecordId, CancellationToken cancellationToken = default);

    /// <summary>Returns renderer-neutral identity and ordered position data for one tracking record.</summary>
    Task<TrackingProfileData?> GetProfileDataAsync(int trackingRecordId, CancellationToken cancellationToken = default);
}
