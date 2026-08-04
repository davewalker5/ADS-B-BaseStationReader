using BaseStationReader.Entities.Tracking;

#nullable enable

namespace BaseStationReader.Entities.History;

/// <summary>
/// Represents an immutable persisted position-density snapshot.
/// </summary>
public sealed class PositionDensitySnapshotEntity
{
    public int Id { get; set; }
    public int SessionId { get; set; }
    public DateTime CapturedAtUtc { get; set; }
    public int PositionCount { get; set; }
    public int MaximumBinCount { get; set; }
    public double MinimumLatitude { get; set; }
    public double MaximumLatitude { get; set; }
    public double MinimumLongitude { get; set; }
    public double MaximumLongitude { get; set; }
    public ObservationSession Session { get; set; } = null!;
    public ICollection<PositionDensitySnapshotCellEntity> Cells { get; set; } = [];
}

/// <summary>
/// Represents one populated geographic bin in a persisted position-density snapshot.
/// </summary>
public sealed class PositionDensitySnapshotCellEntity
{
    public int Id { get; set; }
    public int PositionDensitySnapshotId { get; set; }
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public int Count { get; set; }
    public PositionDensitySnapshotEntity PositionDensitySnapshot { get; set; } = null!;
}
