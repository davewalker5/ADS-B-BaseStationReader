#nullable enable

using BaseStationReader.Entities.Hub;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Exposes live tracker state without leaking SignalR transport details to components.
/// </summary>
public interface ILiveAircraftService : IAsyncDisposable
{
    IReadOnlyCollection<TrackedAircraftDto> Aircraft { get; }
    TrackingOptions? TrackingOptions { get; }
    ConnectionState ConnectionState { get; }
    DateTimeOffset? LastUpdated { get; }
    event EventHandler? StateChanged;

    /// <summary>
    /// Starts the live connection and loads its authoritative snapshot.
    /// </summary>
    Task StartAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Replaces local state with the tracker's authoritative current snapshot.
    /// </summary>
    Task RefreshAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Stops the live connection.
    /// </summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
