namespace BaseStationReader.TrackerHub.Services;

public interface ITrackingControlService
{
    bool IsTracking { get; }
    Task StartAsync(string receiverHost, int receiverPort, string notes = null,
        CancellationToken cancellationToken = default);
    Task StopAsync(CancellationToken cancellationToken = default);
}
