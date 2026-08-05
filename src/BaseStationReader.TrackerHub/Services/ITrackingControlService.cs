namespace BaseStationReader.TrackerHub.Services;

public interface ITrackingControlService
{
    bool IsTracking { get; }
    /// <summary>Starts a tracking session using the supplied receiver endpoint.</summary>
    Task StartAsync(string receiverHost, int receiverPort, string sessionName, string notes = null,
        CancellationToken cancellationToken = default);
    /// <summary>Stops the active tracking session.</summary>
    Task StopAsync(CancellationToken cancellationToken = default);
}
