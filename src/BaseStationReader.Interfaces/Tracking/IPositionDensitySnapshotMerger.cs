#nullable enable

using BaseStationReader.Entities.History;

namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Merges refreshed position-density calculations into an existing snapshot.
/// </summary>
public interface IPositionDensitySnapshotMerger
{
    /// <summary>
    /// Merges a refreshed calculation without allowing occupied cells or recorded counts to disappear.
    /// </summary>
    PositionDensityDto? Merge(PositionDensityDto? current, PositionDensityDto? refreshed);
}
