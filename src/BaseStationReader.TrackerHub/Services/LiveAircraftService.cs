#nullable enable

using BaseStationReader.Entities.Hub;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Maintains a reconnecting SignalR connection and an authoritative aircraft collection.
/// </summary>
public sealed class LiveAircraftService : ILiveAircraftService
{
    private readonly NavigationManager _navigation;
    private readonly string? _hubUrl;
    private readonly Dictionary<string, TrackedAircraftDto> _aircraft = new(StringComparer.OrdinalIgnoreCase);
    private readonly List<string> _aircraftOrder = [];
    private readonly object _stateLock = new();
    private readonly SemaphoreSlim _gate = new(1, 1);
    private HubConnection? _connection;
    private bool _disposed;

    public IReadOnlyCollection<TrackedAircraftDto> Aircraft
    {
        get
        {
            // Return a point-in-time copy in explicit arrival order so updates cannot move existing rows.
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

    /// <summary>
    /// Initialises the service with the URI of the current Tracker Hub host.
    /// </summary>
    /// <param name="navigation">The current Blazor navigation service.</param>
    /// <param name="configuration">Tracker Hub configuration.</param>
    public LiveAircraftService(NavigationManager navigation, IConfiguration configuration)
    {
        _navigation = navigation;
        _hubUrl = configuration["WebUi:SignalRHubUrl"];
    }

    /// <inheritdoc />
    public async Task StartAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            // A scoped service may be requested by multiple components, so only create one connection.
            if (_connection is not null)
            {
                return;
            }

            SetConnectionState(ConnectionState.Connecting);
            _connection = BuildConnection();
            await _connection.StartAsync(cancellationToken);

            // Always replace local state with a fresh snapshot after establishing the transport.
            await RefreshSnapshotAsync(cancellationToken);
            SetConnectionState(ConnectionState.Connected);
        }
        catch
        {
            // Dispose a partially started connection so a user retry can build a clean transport.
            if (_connection is not null)
            {
                await _connection.DisposeAsync();
                _connection = null;
            }

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
            // Stop and release the transport while retaining state for a graceful final render.
            if (_connection is not null)
            {
                await _connection.StopAsync(cancellationToken);
                await _connection.DisposeAsync();
                _connection = null;
            }

            SetConnectionState(ConnectionState.Disconnected);
        }
        finally
        {
            _gate.Release();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        // Disposal is idempotent because Blazor can tear down a circuit during connection recovery.
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        await StopAsync();
        _gate.Dispose();
    }

    /// <summary>
    /// Creates the SignalR connection and registers all state handlers before it starts.
    /// </summary>
    /// <returns>An unstarted, configured hub connection.</returns>
    private HubConnection BuildConnection()
    {
        var hubUri = string.IsNullOrWhiteSpace(_hubUrl)
            ? _navigation.ToAbsoluteUri("/hubs/aircraft")
            : new Uri(_hubUrl, UriKind.Absolute);
        var connection = new HubConnectionBuilder()
            .WithUrl(hubUri)
            .WithAutomaticReconnect()
            .Build();

        // Register handlers before StartAsync so no incremental message can be missed.
        connection.On<TrackedAircraftDto>("aircraftUpdate", ApplyUpdate);
        connection.On<TrackedAircraftDto>("aircraftRemoved", ApplyRemoval);
        connection.Reconnecting += OnReconnectingAsync;
        connection.Reconnected += OnReconnectedAsync;
        connection.Closed += OnClosedAsync;
        return connection;
    }

    /// <summary>
    /// Replaces local aircraft and profile data with authoritative hub values.
    /// </summary>
    /// <param name="cancellationToken">Cancels the hub calls.</param>
    private async Task RefreshSnapshotAsync(CancellationToken cancellationToken)
    {
        // Capture the connection so nullable analysis and both calls use the same transport instance.
        var connection = _connection ?? throw new InvalidOperationException("The Tracker Hub connection is not available.");
        var snapshot = await connection.InvokeAsync<IReadOnlyCollection<TrackedAircraftDto>>(
            "GetCurrentAircraft", cancellationToken);
        var options = await connection.InvokeAsync<TrackingOptions>("GetTrackingOptions", cancellationToken);

        // Replace rather than merge so removals missed during an outage cannot survive reconnection.
        lock (_stateLock)
        {
            _aircraft.Clear();
            _aircraftOrder.Clear();
            foreach (var aircraft in snapshot)
            {
                if (!_aircraft.ContainsKey(aircraft.Address))
                {
                    // Preserve the authoritative snapshot order while guarding against duplicate addresses.
                    _aircraftOrder.Add(aircraft.Address);
                }

                _aircraft[aircraft.Address] = aircraft;
            }
        }

        TrackingOptions = options;
        LastUpdated = DateTimeOffset.Now;
        NotifyStateChanged();
    }

    /// <summary>
    /// Adds or replaces a single live aircraft update.
    /// </summary>
    /// <param name="aircraft">The latest aircraft state.</param>
    private void ApplyUpdate(TrackedAircraftDto aircraft)
    {
        // ICAO address is the stable identity shared by snapshots and incremental updates.
        lock (_stateLock)
        {
            if (!_aircraft.ContainsKey(aircraft.Address))
            {
                // Append a newly observed aircraft once; replacing later DTOs leaves its position unchanged.
                _aircraftOrder.Add(aircraft.Address);
            }

            _aircraft[aircraft.Address] = aircraft;
        }
        LastUpdated = DateTimeOffset.Now;
        NotifyStateChanged();
    }

    /// <summary>
    /// Removes an aircraft that has left the tracker collection.
    /// </summary>
    /// <param name="aircraft">The removed aircraft payload.</param>
    private void ApplyRemoval(TrackedAircraftDto aircraft)
    {
        // Ignore duplicate removal notifications while still recording transport activity.
        lock (_stateLock)
        {
            if (_aircraft.Remove(aircraft.Address))
            {
                // Remove ordering state with the aircraft so a future reappearance is treated as newly added.
                _aircraftOrder.RemoveAll(address => address.Equals(aircraft.Address, StringComparison.OrdinalIgnoreCase));
            }
        }
        LastUpdated = DateTimeOffset.Now;
        NotifyStateChanged();
    }

    /// <summary>
    /// Marks the service as reconnecting after an interrupted connection.
    /// </summary>
    /// <param name="exception">The interruption error, when available.</param>
    private Task OnReconnectingAsync(Exception? exception)
    {
        // Preserve the last known table while clearly warning that it is not live.
        SetConnectionState(ConnectionState.Reconnecting);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Reloads authoritative state after SignalR reconnects.
    /// </summary>
    /// <param name="connectionId">The new SignalR connection identifier.</param>
    private async Task OnReconnectedAsync(string? connectionId)
    {
        try
        {
            // Resynchronise before declaring the connection healthy.
            await RefreshSnapshotAsync(CancellationToken.None);
            SetConnectionState(ConnectionState.Connected);
        }
        catch
        {
            SetConnectionState(ConnectionState.Disconnected);
        }
    }

    /// <summary>
    /// Marks the service disconnected when automatic reconnection gives up.
    /// </summary>
    /// <param name="exception">The terminal connection error, when available.</param>
    private Task OnClosedAsync(Exception? exception)
    {
        // Keep the last snapshot visible but label it as disconnected.
        SetConnectionState(ConnectionState.Disconnected);
        return Task.CompletedTask;
    }

    /// <summary>
    /// Updates connection status and notifies interested components.
    /// </summary>
    /// <param name="state">The new connection state.</param>
    private void SetConnectionState(ConnectionState state)
    {
        ConnectionState = state;
        NotifyStateChanged();
    }

    /// <summary>
    /// Raises the state-change event on the current service scope.
    /// </summary>
    private void NotifyStateChanged()
    {
        // Copy the delegate before invocation to avoid a subscription race.
        var handler = StateChanged;
        handler?.Invoke(this, EventArgs.Empty);
    }
}
