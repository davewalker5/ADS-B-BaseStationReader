using BaseStationReader.Entities.History;

namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Maps an in-memory position-density snapshot to its persistence representation.
/// </summary>
public interface IPositionDensitySnapshotMapper
{
    /// <summary>
    /// Maps a captured snapshot without recalculating its analytical values.
    /// </summary>
    /// <param name="snapshot"></param>
    /// <param name="capturedAtUtc"></param>
    /// <returns></returns>
    /// <exception cref="ArgumentNullException"></exception>
    /// <exception cref="ArgumentException"></exception>
    PositionDensitySnapshotEntity Map(PositionDensity snapshot, DateTime capturedAtUtc);
}
