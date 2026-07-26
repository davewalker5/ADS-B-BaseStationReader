using BaseStationReader.Interfaces.Hub;

namespace BaseStationReader.TrackerHub.Services;

public sealed class TrackingControlService(TrackingRuntime runtime, IEventBridge bridge) : ITrackingControlService
{
    public bool IsTracking => runtime.IsTracking;

    public async Task StartAsync(string notes = null, CancellationToken cancellationToken = default)
    {
        await runtime.StartTrackingAsync(notes, cancellationToken);
        await bridge.PublishResetAsync(runtime.TrackingOptions, cancellationToken);
    }

    public async Task StopAsync(CancellationToken cancellationToken = default)
    {
        await runtime.StopTrackingAsync(cancellationToken);
        await bridge.PublishResetAsync(runtime.TrackingOptions, cancellationToken);
    }
}
