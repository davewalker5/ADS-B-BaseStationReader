#nullable enable

using BaseStationReader.Interfaces.Database;
using BaseStationReader.TrackerHub.Models;
using Microsoft.Extensions.Options;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Adapts business-logic historical queries to UI-specific chart and map preparation.
/// </summary>
public sealed class TrackingSessionQueryService(
    ITrackingSessionQueryManager manager,
    IFlightProfileBuilder flightProfileBuilder,
    IFlightPathBuilder flightPathBuilder,
    IOptions<DatabaseBrowserOptions> options) : ITrackingSessionQueryService
{
    /// <inheritdoc />
    public Task<int?> GetLatestSessionIdAsync(CancellationToken cancellationToken = default)
    {
        // Forward scalar historical reads without adding UI persistence behavior.
        return manager.GetLatestSessionIdAsync(cancellationToken);
    }

    /// <inheritdoc />
    public Task<ObservationSessionSummaryDto?> GetObservationSessionSummaryAsync(int sessionId, CancellationToken cancellationToken = default)
    {
        // Forward the renderer-neutral aggregate produced by business logic.
        return manager.GetObservationSessionSummaryAsync(sessionId, cancellationToken);
    }

    /// <inheritdoc />
    public Task<IReadOnlyList<ObservationSessionOptionDto>> ListSessionsAsync(CancellationToken cancellationToken = default)
    {
        // Apply the UI-configured history window at the manager boundary.
        return manager.ListSessionsAsync(options.Value.SessionHistoryDays, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedResult<ObservationSessionDto>> SearchObservationSessionsAsync(ObservationSessionFilter filter, CancellationToken cancellationToken = default)
    {
        // Business logic owns validation, filtering, paging, and projection.
        return manager.SearchObservationSessionsAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<PagedResult<TrackingSessionSummaryDto>> SearchAsync(TrackingSessionFilter filter, CancellationToken cancellationToken = default)
    {
        // Business logic owns the bounded multi-table historical search.
        return manager.SearchAsync(filter, cancellationToken);
    }

    /// <inheritdoc />
    public Task<TrackingSessionDetailDto?> GetAsync(int trackingRecordId, CancellationToken cancellationToken = default)
    {
        // Return the business-logic detail projection unchanged.
        return manager.GetAsync(trackingRecordId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<FlightProfileDto?> GetFlightProfileAsync(int trackingRecordId, CancellationToken cancellationToken = default)
    {
        // Query renderer-neutral points before applying chart-specific preparation in the UI layer.
        var data = await manager.GetProfileDataAsync(trackingRecordId, cancellationToken);
        return data is null
            ? null
            : flightProfileBuilder.Build(data.Id, data.Address, data.Callsign, data.Points);
    }

    /// <inheritdoc />
    public async Task<FlightPathDto?> GetFlightPathAsync(int trackingRecordId, CancellationToken cancellationToken = default)
    {
        // Combine business-logic detail and position reads before applying map-specific preparation.
        var detail = await manager.GetAsync(trackingRecordId, cancellationToken);
        if (detail is null) return null;
        var data = await manager.GetProfileDataAsync(trackingRecordId, cancellationToken);
        if (data is null) return null;

        var flightNumber = !string.IsNullOrWhiteSpace(detail.FlightIata) ? detail.FlightIata : detail.FlightIcao;
        var route = string.IsNullOrWhiteSpace(detail.Embarkation) && string.IsNullOrWhiteSpace(detail.Destination)
            ? string.Empty
            : $"{detail.Embarkation} → {detail.Destination}";
        return flightPathBuilder.Build(
            detail.Id, detail.Address, detail.Callsign, detail.Registration, detail.ModelName,
            flightNumber, detail.AirlineName, route, data.Points);
    }
}
