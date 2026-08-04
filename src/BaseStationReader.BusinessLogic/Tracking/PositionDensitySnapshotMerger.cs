#nullable enable

using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Maintains a monotonic position-density snapshot while one Live Tracker session remains selected.
/// </summary>
public sealed class PositionDensitySnapshotMerger : IPositionDensitySnapshotMerger
{
    /// <summary>
    /// Merges a refreshed calculation without allowing occupied cells or recorded counts to disappear.
    /// </summary>
    /// <param name="current">The density snapshot already displayed for the current session.</param>
    /// <param name="refreshed">The latest complete persisted-position calculation.</param>
    /// <returns>A monotonic snapshot, or the refreshed result when the session has changed.</returns>
    public PositionDensity? Merge(PositionDensity? current, PositionDensity? refreshed)
    {
        if (refreshed is null)
        {
            return null;
        }
        if (current is null || current.SessionId != refreshed.SessionId)
        {
            return refreshed;
        }

        // Stable session bounds make geographic bin centres safe identities across recalculations.
        var bins = current.Bins.ToDictionary(bin => (bin.Latitude, bin.Longitude));
        foreach (var refreshedBin in refreshed.Bins)
        {
            var key = (refreshedBin.Latitude, refreshedBin.Longitude);
            if (!bins.TryGetValue(key, out var existing) || refreshedBin.Count > existing.Count)
            {
                bins[key] = refreshedBin;
            }
        }

        var mergedBins = bins.Values
            .OrderBy(bin => bin.Latitude)
            .ThenBy(bin => bin.Longitude)
            .ToArray();
        return new PositionDensity
        {
            SessionId = refreshed.SessionId,
            PositionCount = Math.Max(current.PositionCount, refreshed.PositionCount),
            MaximumBinCount = mergedBins.Length == 0 ? 0 : mergedBins.Max(bin => bin.Count),
            MinimumLatitude = refreshed.MinimumLatitude,
            MaximumLatitude = refreshed.MaximumLatitude,
            MinimumLongitude = refreshed.MinimumLongitude,
            MaximumLongitude = refreshed.MaximumLongitude,
            Bins = mergedBins
        };
    }
}
