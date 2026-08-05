using BaseStationReader.Interfaces.Hub;

namespace BaseStationReader.TrackerHub.Services;

public sealed class TrackingControlService(TrackingRuntime runtime, IEventBridge bridge) : ITrackingControlService
{
    public bool IsTracking => runtime.IsTracking;

    /// <inheritdoc />
    public async Task StartAsync(string receiverHost, int receiverPort, string sessionName, string notes = null,
        CancellationToken cancellationToken = default)
    {
        await runtime.StartTrackingAsync(receiverHost, receiverPort, sessionName, notes, cancellationToken);
        await bridge.PublishResetAsync(runtime.TrackingOptions, cancellationToken);
    }

    /// <inheritdoc />
    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await runtime.StopTrackingAsync(cancellationToken);
        await bridge.PublishResetAsync(runtime.TrackingOptions, cancellationToken);
    }
}
