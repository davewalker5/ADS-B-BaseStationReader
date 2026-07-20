namespace BaseStationReader.TrackerHub.Services;

using BaseStationReader.TrackerHub.Models;

public interface ITrackingProfileService
{
    IReadOnlyList<TrackingProfileOption> List();
    Task ApplyAsync(string fileName, CancellationToken cancellationToken = default);
}
