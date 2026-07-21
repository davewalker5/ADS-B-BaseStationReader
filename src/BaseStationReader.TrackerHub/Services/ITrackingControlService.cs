namespace BaseStationReader.TrackerHub.Services;

public interface ITrackingControlService
{
    bool IsTracking { get; }
    Task StartAsync(CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
