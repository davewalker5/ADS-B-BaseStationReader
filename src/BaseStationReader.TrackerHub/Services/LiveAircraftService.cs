#nullable enable

using BaseStationReader.Entities.Events;
using BaseStationReader.Entities.Hub;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Maintains circuit-local live aircraft state directly from the in-process tracking runtime.
/// </summary>
public sealed class LiveAircraftService : ILiveAircraftService
{
    private readonly ITrackerController _controller;
    private readonly TimeSpan _stateChangeInterval;
    private readonly Dictionary<string, TrackedAircraftDto> _aircraft = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _aircraftOrder = [];
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private readonly CancellationTokenSource _notificationCancellation = new();
    private bool _started;
    private bool _disposed;
    private int _notificationScheduled;

    public IReadOnlyCollection<TrackedAircraftDto> Aircraft
    {
        get
        {
            lock (_stateLock)
            {
                return _aircraftOrder
                    .Where(_aircraft.ContainsKey)
                    .Select(address => _aircraft[address])
                    .ToArray();
            }
        }
    }

    public TrackingOptions? TrackingOptions { get; private set; }
    public ConnectionState ConnectionState { get; private set; } = ConnectionState.Disconnected;
    public DateTimeOffset? LastUpdated { get; private set; }
    public event EventHandler? StateChanged;

    public LiveAircraftService(ITrackerController controller, IConfiguration configuration)
    {
        _controller = controller;
        var refreshInterval = configuration.GetValue<int?>("ApplicationSettings:RefreshInterval") ?? 1000;
        _stateChangeInterval = TimeSpan.FromMilliseconds(Math.Max(100, refreshInterval));
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_started)
            {
                return;
            }

            SetConnectionState(ConnectionState.Connecting);
            _controller.AircraftEvent += OnAircraftEvent;
            RefreshSnapshot();
            _started = true;
            SetConnectionState(ConnectionState.Connected);
        }
        catch
        {
            _controller.AircraftEvent -= OnAircraftEvent;
            SetConnectionState(ConnectionState.Disconnected);
            throw;
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (_started)
            {
                _controller.AircraftEvent -= OnAircraftEvent;
                _started = false;
            }

            SetConnectionState(ConnectionState.Disconnected);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async Task RefreshAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!_started)
            {
                throw new InvalidOperationException("The live aircraft service is not running.");
            }

            RefreshSnapshot();
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _notificationCancellation.Cancel();
        await StopAsync();
        _notificationCancellation.Dispose();
        _gate.Dispose();
    }

    private void RefreshSnapshot()
    {
        var snapshot = _controller.State.ToArray();
        lock (_stateLock)
        {
            _aircraft.Clear();
            _aircraftOrder.Clear();
            foreach (var aircraft in snapshot)
            {
                if (!_aircraft.ContainsKey(aircraft.Address))
                {
                    _aircraftOrder.Add(aircraft.Address);
                }

                _aircraft[aircraft.Address] = aircraft;
            }
        }

        TrackingOptions = _controller.TrackingOptions;
        LastUpdated = DateTimeOffset.Now;
        NotifyStateChanged();
    }

    private void OnAircraftEvent(object? sender, AircraftNotificationEventArgs args)
    {
        if (args.Aircraft is null)
        {
            return;
        }

        var aircraft = TrackedAircraftDto.FromTrackedAircraft(args.Aircraft);
        if (args.NotificationType == AircraftNotificationType.Removed)
        {
            ApplyRemoval(aircraft);
        }
        else if (args.NotificationType != AircraftNotificationType.Unknown)
        {
            ApplyUpdate(aircraft);
        }
    }

    internal void ApplyUpdate(TrackedAircraftDto aircraft)
    {
        lock (_stateLock)
        {
            if (!_aircraft.ContainsKey(aircraft.Address))
            {
                _aircraftOrder.Add(aircraft.Address);
            }

            _aircraft[aircraft.Address] = aircraft;
        }

        LastUpdated = DateTimeOffset.Now;
        NotifyStateChanged();
    }

    private void ApplyRemoval(TrackedAircraftDto aircraft)
    {
        lock (_stateLock)
        {
            if (_aircraft.Remove(aircraft.Address))
            {
                _aircraftOrder.RemoveAll(address =>
                    address.Equals(aircraft.Address, StringComparison.OrdinalIgnoreCase));
            }
        }

        LastUpdated = DateTimeOffset.Now;
        NotifyStateChanged();
    }

    private void SetConnectionState(ConnectionState state)
    {
        ConnectionState = state;
        NotifyStateChanged();
    }

    private void NotifyStateChanged()
    {
        if (_disposed || Interlocked.CompareExchange(ref _notificationScheduled, 1, 0) != 0)
        {
            return;
        }

        _ = NotifyStateChangedAsync(_notificationCancellation.Token);
    }

    private async Task NotifyStateChangedAsync(CancellationToken cancellationToken)
    {
        try
        {
            await Task.Delay(_stateChangeInterval, cancellationToken);
            Interlocked.Exchange(ref _notificationScheduled, 0);
            StateChanged?.Invoke(this, EventArgs.Empty);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            Interlocked.Exchange(ref _notificationScheduled, 0);
        }
    }
}
