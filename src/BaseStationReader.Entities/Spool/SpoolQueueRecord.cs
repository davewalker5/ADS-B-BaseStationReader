using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Tracking;

#nullable enable

namespace BaseStationReader.Entities.Spool;

/// <summary>
/// Contains one self-contained database write request persisted in the spool.
/// </summary>
public sealed class SpoolQueueRecord
{
    public Guid Id { get; init; }

    public DateTime QueuedAtUtc { get; init; }

    public SpoolEntityType EntityType { get; init; }

    public TrackedAircraft? TrackedAircraft { get; init; }

    public AircraftPosition? AircraftPosition { get; init; }

    public PositionDensitySnapshotEntity? PositionDensitySnapshot { get; init; }
}
