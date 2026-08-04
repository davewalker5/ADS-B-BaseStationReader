#nullable enable

using System.Collections.Concurrent;
using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Periodically aggregates positions observed during the active tracking session.
/// </summary>
public sealed class PositionDensitySnapshotOrchestrator : IPositionDensitySnapshotOrchestrator
{
    private readonly IPositionDensityAggregator _aggregator;
    private readonly IPositionDensitySnapshotStateManager _stateManager;
    private readonly object _sync = new();
    private ConcurrentQueue<PositionDensityCoordinate> _coordinates = new();
    private CancellationTokenSource? _cancellation;
    private Task? _runTask;
    private int _sessionId;
    private PositionDensityBounds _bounds;

    /// <summary>
    /// Creates a periodic position-density snapshot orchestrator.
    /// </summary>
    /// <param name="aggregator"></param>
    /// <param name="stateManager"></param>
    /// <exception cref="ArgumentNullException"></exception>
    public PositionDensitySnapshotOrchestrator(
        IPositionDensityAggregator aggregator,
        IPositionDensitySnapshotStateManager stateManager)
    {
        ArgumentNullException.ThrowIfNull(aggregator);
        ArgumentNullException.ThrowIfNull(stateManager);
        _aggregator = aggregator;
        _stateManager = stateManager;
    }

    /// <inheritdoc />
    public void Start(
        int sessionId,
        PositionDensityBounds bounds,
        TimeSpan interval,
        CancellationToken cancellationToken)
    {
        if (sessionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId));
        }
        if (interval <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(interval));
        }

        lock (_sync)
        {
            if (_runTask is { IsCompleted: false })
            {
                throw new InvalidOperationException("Position-density snapshot orchestration is already running.");
            }

            // A new tracking session must never inherit positions or density state from its predecessor.
            _coordinates = new ConcurrentQueue<PositionDensityCoordinate>();
            _stateManager.Clear();
            _sessionId = sessionId;
            _bounds = bounds;
            _cancellation = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            _runTask = RunAsync(interval, _cancellation.Token);
        }
    }

    /// <inheritdoc />
    public void Record(AircraftPosition? position)
    {
        if (position?.Latitude is not { } latitude || position.Longitude is not { } longitude ||
            latitude is < -90 or > 90 || longitude is < -180 or > 180)
        {
            return;
        }

        lock (_sync)
        {
            if (_runTask is not { IsCompleted: false })
            {
                return;
            }

            // Retain every accepted observation so each interval produces a complete session snapshot.
            _coordinates.Enqueue(new PositionDensityCoordinate((double)latitude, (double)longitude));
        }
    }

    /// <inheritdoc />
    public async Task StopAsync()
    {
        Task? runTask;
        CancellationTokenSource? cancellation;
        lock (_sync)
        {
            runTask = _runTask;
            cancellation = _cancellation;
            _runTask = null;
            _cancellation = null;
        }

        if (runTask is null || cancellation is null)
        {
            return;
        }

        cancellation.Cancel();
        try
        {
            // Observe loop completion so no periodic update can outlive its tracking session.
            await runTask.ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
            // Cancellation is the normal session shutdown path.
        }
        finally
        {
            cancellation.Dispose();
            _coordinates = new ConcurrentQueue<PositionDensityCoordinate>();
        }
    }

    /// <summary>
    /// Runs periodic complete-session aggregation until tracking stops.
    /// </summary>
    /// <param name="interval"></param>
    /// <param name="cancellationToken"></param>
    /// <returns></returns>
    private async Task RunAsync(TimeSpan interval, CancellationToken cancellationToken)
    {
        using var timer = new PeriodicTimer(interval);
        while (await timer.WaitForNextTickAsync(cancellationToken).ConfigureAwait(false))
        {
            // Snapshot the concurrent input before CPU-bound aggregation to keep recording non-blocking.
            var coordinates = _coordinates.ToArray();
            var density = _aggregator.Aggregate(_sessionId, coordinates, _bounds);
            _stateManager.Merge(density);
        }
    }
}
