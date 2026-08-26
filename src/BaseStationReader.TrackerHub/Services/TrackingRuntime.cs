#nullable enable

using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Hub;
using BaseStationReader.Entities.Spool;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>Stable facade around the controller that may be replaced when a profile changes.</summary>
public sealed class TrackingRuntime : ITrackerController, IReceiverPositionProvider, ILiveTrackerStatisticsProvider
{
    private readonly Func<TrackerApplicationSettings, string?, string?, ITrackerController> _factory;
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly object _stateLock = new();
    private TrackerApplicationSettings _settings;
    private ITrackerController? _controller;
    private CancellationTokenSource? _controllerCancellation;
    private Task? _controllerTask;
    private CancellationToken _applicationToken;
    private bool _started;
    private long _lastMessagesProcessed;
    private long _lastPositionRecordsWritten;
    private long _lastPositionRecordsObserved;
    private long _lastDistinctAircraft;
    private long _lastDistinctCallsigns;
    private long _lastAircraftWithPositionRecords;
    private long _lastAircraftWithPositionObservations;
    private long _lastActivityUtcTicks;

    public TrackingRuntime(TrackerApplicationSettings settings,
        Func<TrackerApplicationSettings, string?, string?, ITrackerController> factory)
    {
        _settings = settings;
        _factory = factory;
    }

    public event EventHandler<AircraftNotificationEventArgs>? AircraftEvent;
    public event EventHandler<MessageReadEventArgs>? MessageReceived;
    public IEnumerable<TrackedAircraftDto> State { get { lock (_stateLock) return _controller?.State.ToArray() ?? []; } }
    public TrackingOptions TrackingOptions { get { lock (_stateLock) return TrackingOptions.FromTrackerSettings(_settings); } }
    public int QueueSize { get { lock (_stateLock) return _controller?.QueueSize ?? 0; } }
    public long MessagesProcessed { get { lock (_stateLock) return _controller?.MessagesProcessed ?? _lastMessagesProcessed; } }
    public long PositionRecordsWritten { get { lock (_stateLock) return _controller?.PositionRecordsWritten ?? _lastPositionRecordsWritten; } }
    public long PositionRecordsObserved { get { lock (_stateLock) return _controller?.PositionRecordsObserved ?? _lastPositionRecordsObserved; } }
    public long DistinctAircraft { get { lock (_stateLock) return _controller?.DistinctAircraft ?? _lastDistinctAircraft; } }
    public long DistinctCallsigns { get { lock (_stateLock) return _controller?.DistinctCallsigns ?? _lastDistinctCallsigns; } }
    public long AircraftWithPositionRecords { get { lock (_stateLock) return _controller?.AircraftWithPositionRecords ?? _lastAircraftWithPositionRecords; } }
    public long AircraftWithPositionObservations { get { lock (_stateLock) return _controller?.AircraftWithPositionObservations ?? _lastAircraftWithPositionObservations; } }
    public DateTime? LastActivityUtc
    {
        get
        {
            var ticks = Interlocked.Read(ref _lastActivityUtcTicks);
            return ticks == 0 ? null : new DateTime(ticks, DateTimeKind.Utc);
        }
    }
    public bool IsTracking { get { lock (_stateLock) return _controller is not null; } }
    public (double? Latitude, double? Longitude) ReceiverPosition
    {
        get { lock (_stateLock) return (_settings.ReceiverLatitude, _settings.ReceiverLongitude); }
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken token)
    {
        _applicationToken = token;
        await _gate.WaitAsync(token);
        try
        {
            _started = true;
        }
        finally { _gate.Release(); }

        try { await Task.Delay(Timeout.InfiniteTimeSpan, token); }
        catch (OperationCanceledException) when (token.IsCancellationRequested) { }
        await StopControllerAsync();
        _started = false;
    }

    /// <summary>
    /// Starts a replaceable tracker controller for a new observation session.
    /// </summary>
    public async Task StartTrackingAsync(string receiverHost, int receiverPort, string sessionName, string? notes = null,
        CancellationToken token = default,
        Func<TrackingOptions, CancellationToken, ValueTask>? beforeStart = null)
    {
        if (string.IsNullOrWhiteSpace(receiverHost))
        {
            throw new ArgumentException("Receiver host is required.", nameof(receiverHost));
        }
        if (receiverPort is < 1 or > 65535)
        {
            throw new ArgumentOutOfRangeException(nameof(receiverPort), "Receiver port must be between 1 and 65535.");
        }
        if (string.IsNullOrWhiteSpace(sessionName) || sessionName.Trim().Length > 100)
        {
            throw new ArgumentException("Session name is required and cannot exceed 100 characters.", nameof(sessionName));
        }

        await _gate.WaitAsync(token);
        try
        {
            if (!_started)
            {
                throw new InvalidOperationException("The tracking runtime is not ready.");
            }
            if (!IsTracking)
            {
                lock (_stateLock)
                {
                    _settings.Host = receiverHost.Trim();
                    _settings.Port = receiverPort;
                }
                if (beforeStart is not null)
                {
                    // Publish session-boundary state before the controller can emit its first aircraft update.
                    await beforeStart(TrackingOptions, token);
                }
                StartController(sessionName.Trim(), notes);
            }
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Stops the active tracker controller, if present.
    /// </summary>
    public async Task StopTrackingAsync(bool? flushQueue = null, CancellationToken token = default,
        IProgress<QueueFlushProgress>? progress = null)
    {
        await _gate.WaitAsync(token);
        try
        {
            ITrackerController? controller;
            lock (_stateLock) controller = _controller;
            if (flushQueue.HasValue)
            {
                controller?.ConfigureStopFlush(flushQueue.Value, token, progress);
            }
            await StopControllerCoreAsync(flushQueue == false);
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Replaces the effective tracker settings while no session is active.
    /// </summary>
    public async Task ApplyAsync(TrackerApplicationSettings settings, CancellationToken token = default)
    {
        await _gate.WaitAsync(token);
        try
        {
            if (IsTracking)
            {
                throw new InvalidOperationException("The tracking profile cannot be changed during an active session.");
            }
            lock (_stateLock) _settings = settings;
        }
        finally { _gate.Release(); }
    }

    /// <summary>
    /// Executes a database-editing action while serialising it against tracking state changes.
    /// </summary>
    internal async Task ExecuteWhileIdleAsync(Func<Task> action, CancellationToken token = default)
    {
        ArgumentNullException.ThrowIfNull(action);
        await _gate.WaitAsync(token);
        try
        {
            if (IsTracking)
            {
                throw new InvalidOperationException("Sessions cannot be edited while a tracking session is active.");
            }
            await action();
        }
        finally { _gate.Release(); }
    }

    /// <inheritdoc />
    public async Task FlushQueueAsync(CancellationToken cancellationToken = default,
        IProgress<QueueFlushProgress>? progress = null)
    {
        ITrackerController? controller;
        lock (_stateLock) controller = _controller;
        if (controller is not null)
        {
            await controller.FlushQueueAsync(cancellationToken, progress);
        }
    }

    /// <summary>
    /// Creates and starts the replaceable controller for a new observation session.
    /// </summary>
    /// <param name="notes">Optional notes recorded with the new session.</param>
    private void StartController(string sessionName, string? notes = null)
    {
        // A new controller represents a new observation session, so reset the retained final count.
        if (_applicationToken.IsCancellationRequested)
        {
            return;
        }
        var controller = _factory(_settings, sessionName, notes);
        controller.AircraftEvent += ForwardAircraftEvent;
        controller.MessageReceived += ForwardMessageReceived;
        var cancellation = CancellationTokenSource.CreateLinkedTokenSource(_applicationToken);
        lock (_stateLock)
        {
            Interlocked.Exchange(ref _lastActivityUtcTicks, 0);
            _lastMessagesProcessed = 0;
            _lastPositionRecordsWritten = 0;
            _lastPositionRecordsObserved = 0;
            _lastDistinctAircraft = 0;
            _lastDistinctCallsigns = 0;
            _lastAircraftWithPositionRecords = 0;
            _lastAircraftWithPositionObservations = 0;
            _controller = controller;
            _controllerCancellation = cancellation;
            // A session can be started from a Blazor circuit. Run the long-lived controller outside
            // that circuit's synchronization context so message processing cannot monopolise the UI
            // dispatcher and delay renders until the page is manually reloaded.
            _controllerTask = Task.Run(() => controller.StartAsync(cancellation.Token), CancellationToken.None);
        }
    }

    private async Task StopControllerAsync()
    {
        await _gate.WaitAsync();
        try { await StopControllerCoreAsync(); }
        finally { _gate.Release(); }
    }

    private async Task StopControllerCoreAsync(bool bounded = false)
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
        if (controller is null)
        {
            return;
        }
        controller.RequestStop();
        cancellation!.Cancel();
        Exception? stopException = null;
        try
        {
            if (task is not null)
            {
                if (bounded)
                {
                    var timeout = TimeSpan.FromMilliseconds(Math.Max(1, _settings.StopTimeout));
                    await task.WaitAsync(timeout);
                }
                else
                {
                    await task;
                }
            }
        }
        catch (OperationCanceledException) { }
        catch (TimeoutException)
        {
            throw new TimeoutException(
                $"The tracking session did not release its resources within {_settings.StopTimeout:N0} ms. " +
                "Shutdown cancellation remains active; check the application log before using the spool replayer.");
        }
        catch (Exception exception)
        {
            // The controller task has completed, albeit unsuccessfully. Release its runtime state
            // before surfacing the failure so the UI cannot remain stuck in an active session.
            stopException = exception;
        }

        controller.AircraftEvent -= ForwardAircraftEvent;
        controller.MessageReceived -= ForwardMessageReceived;
        cancellation.Dispose();
        lock (_stateLock)
        {
            // Preserve the completed session total after releasing its controller.
            _lastMessagesProcessed = controller.MessagesProcessed;
            _lastPositionRecordsWritten = controller.PositionRecordsWritten;
            _lastPositionRecordsObserved = controller.PositionRecordsObserved;
            _lastDistinctAircraft = controller.DistinctAircraft;
            _lastDistinctCallsigns = controller.DistinctCallsigns;
            _lastAircraftWithPositionRecords = controller.AircraftWithPositionRecords;
            _lastAircraftWithPositionObservations = controller.AircraftWithPositionObservations;
            _controller = null;
            _controllerCancellation = null;
            _controllerTask = null;
        }

        if (stopException is not null)
        {
            System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(stopException).Throw();
        }
    }

    private void ForwardAircraftEvent(object? sender, AircraftNotificationEventArgs args)
        => AircraftEvent?.Invoke(this, args);

    private void ForwardMessageReceived(object? sender, MessageReadEventArgs args)
    {
        Interlocked.Exchange(ref _lastActivityUtcTicks, DateTime.UtcNow.Ticks);
        MessageReceived?.Invoke(this, args);
    }
}
