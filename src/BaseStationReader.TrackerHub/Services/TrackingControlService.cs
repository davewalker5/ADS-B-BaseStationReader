using BaseStationReader.Interfaces.Hub;
using BaseStationReader.Entities.Spool;

namespace BaseStationReader.TrackerHub.Services;

public sealed class TrackingControlService(TrackingRuntime runtime, IEventBridge bridge) : ITrackingControlService
{
    public bool IsTracking => runtime.IsTracking;
    public int QueueSize => runtime.QueueSize;
    public bool FlushOnStop => runtime.TrackingOptions.FlushOnStop;

    /// <inheritdoc />
    public async Task StartAsync(string receiverHost, int receiverPort, string sessionName, string notes = null,
        CancellationToken cancellationToken = default)
    {
        await runtime.StartTrackingAsync(
            receiverHost,
            receiverPort,
            sessionName,
            notes,
            cancellationToken,
            bridge.PublishResetAsync);
    }

    /// <inheritdoc />
    public async Task StopAsync(bool? flushQueue = null, CancellationToken cancellationToken = default,
        IProgress<QueueFlushProgress> progress = null)
    {
        await runtime.StopTrackingAsync(flushQueue, cancellationToken, progress);
        // Flush cancellation must not suppress the completed-session reset.
        await bridge.PublishResetAsync(runtime.TrackingOptions, CancellationToken.None);
    }
}
