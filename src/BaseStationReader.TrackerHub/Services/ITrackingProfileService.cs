namespace BaseStationReader.TrackerHub.Services;

using BaseStationReader.TrackerHub.Models;

public interface ITrackingProfileService
{
    /// <summary>Lists the tracking profiles available to the integrated UI.</summary>
    IReadOnlyList<TrackingProfileOption> List();
    /// <summary>Applies a tracking profile while tracking is idle.</summary>
    Task ApplyAsync(string fileName, CancellationToken cancellationToken = default);
}
