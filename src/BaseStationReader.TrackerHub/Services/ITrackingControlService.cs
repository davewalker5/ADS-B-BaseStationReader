namespace BaseStationReader.TrackerHub.Services;

using BaseStationReader.Entities.Spool;

public interface ITrackingControlService
{
    bool IsTracking { get; }
    int QueueSize { get; }
    bool FlushOnStop { get; }
    /// <summary>Starts a tracking session using the supplied receiver endpoint.</summary>
    Task StartAsync(string receiverHost, int receiverPort, string sessionName, string notes = null,
        CancellationToken cancellationToken = default);
    /// <summary>Stops the active tracking session.</summary>
    Task StopAsync(bool? flushQueue = null, CancellationToken cancellationToken = default,
        IProgress<QueueFlushProgress> progress = null);
}
