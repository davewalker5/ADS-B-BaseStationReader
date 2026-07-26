namespace BaseStationReader.TrackerHub.Services;

public interface ITrackingControlService
{
    bool IsTracking { get; }
    Task StartAsync(string notes = null, CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
