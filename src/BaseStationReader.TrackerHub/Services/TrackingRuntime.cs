#nullable enable

using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Hub;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>Stable facade around the controller that may be replaced when a profile changes.</summary>
public sealed class TrackingRuntime : ITrackerController, IReceiverPositionProvider
{
    private readonly Func<TrackerApplicationSettings, ITrackerController> _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _stateLock = new();
    private TrackerApplicationSettings _settings;
    private ITrackerController? _controller;
    private CancellationTokenSource? _controllerCancellation;
    private Task? _controllerTask;
    private CancellationToken _applicationToken;
    private bool _started;

    public TrackingRuntime(TrackerApplicationSettings settings, Func<TrackerApplicationSettings, ITrackerController> factory)
    {
        _settings = settings;
        _factory = factory;
    }

    public event EventHandler<AircraftNotificationEventArgs>? AircraftEvent;
    public IEnumerable<TrackedAircraftDto> State { get { lock (_stateLock) return _controller?.State.ToArray() ?? []; } }
    public TrackingOptions TrackingOptions { get { lock (_stateLock) return TrackingOptions.FromTrackerSettings(_settings); } }
    public int QueueSize { get { lock (_stateLock) return _controller?.QueueSize ?? 0; } }
    public (double? Latitude, double? Longitude) ReceiverPosition
    {
        get { lock (_stateLock) return (_settings.ReceiverLatitude, _settings.ReceiverLongitude); }
    }

    public async Task StartAsync(CancellationToken token)
    {
        _applicationToken = token;
        await _gate.WaitAsync(token);
        try
        {
            _started = true;
            StartController();
        }
        finally { _gate.Release(); }

        try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        await StopControllerAsync();
        _started = false;
    }

    public async Task ApplyAsync(TrackerApplicationSettings settings, CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try
        {
            await StopControllerCoreAsync();
            lock (_stateLock) _settings = settings;
            if (_started) StartController();
        }
        finally { _gate.Release(); }
    }

    public async Task FlushQueueAsync()
    {
        ITrackerController? controller;
        lock (_stateLock) controller = _controller;
        if (controller is not null) await controller.FlushQueueAsync();
    }

    private void StartController()
    {
        if (_applicationToken.IsCancellationRequested) return;
        var controller = _factory(_settings);
        controller.AircraftEvent += ForwardAircraftEvent;
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_applicationToken);
        lock (_stateLock)
        {
            _controller = controller;
            _controllerCancellation = cancellation;
            _controllerTask = controller.StartAsync(cancellation.Token);
        }
    }

    private async Task StopControllerAsync()
    {
        await _gate.WaitAsync();
        try { await StopControllerCoreAsync(); }
        finally { _gate.Release(); }
    }

    private async Task StopControllerCoreAsync()
    {
        ITrackerController? controller;
        CancellationTokenSource? cancellation;
        Task? task;
        lock (_stateLock)
        {
            controller = _controller;
            cancellation = _controllerCancellation;
            task = _controllerTask;
        }
        if (controller is null) return;
        cancellation!.Cancel();
        try { if (task is not null) await task; }
        catch (OperationCanceledException) { }
        controller.AircraftEvent -= ForwardAircraftEvent;
        cancellation.Dispose();
        lock (_stateLock)
        {
            _controller = null;
            _controllerCancellation = null;
            _controllerTask = null;
        }
    }

    private void ForwardAircraftEvent(object? sender, AircraftNotificationEventArgs args)
        => AircraftEvent?.Invoke(this, args);
}
