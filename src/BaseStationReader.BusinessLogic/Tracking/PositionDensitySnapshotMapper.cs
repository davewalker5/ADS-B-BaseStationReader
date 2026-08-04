using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Maps in-memory position-density snapshots to persistence entities.
/// </summary>
public sealed class PositionDensitySnapshotMapper : IPositionDensitySnapshotMapper
{
    /// <inheritdoc />
    public PositionDensitySnapshotEntity Map(PositionDensity snapshot, DateTime capturedAtUtc)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        if (capturedAtUtc.Kind != DateTimeKind.Utc)
        {
            throw new ArgumentException("The snapshot capture time must be UTC.", nameof(capturedAtUtc));
        }

        // Copy every value so subsequent in-memory updates cannot mutate the historical persistence request.
        return new PositionDensitySnapshotEntity
        {
            SessionId = snapshot.SessionId,
            CapturedAtUtc = capturedAtUtc,
            PositionCount = snapshot.PositionCount,
            MaximumBinCount = snapshot.MaximumBinCount,
            MinimumLatitude = snapshot.MinimumLatitude,
            MaximumLatitude = snapshot.MaximumLatitude,
            MinimumLongitude = snapshot.MinimumLongitude,
            MaximumLongitude = snapshot.MaximumLongitude,
            Cells = snapshot.Bins.Select(bin => new PositionDensitySnapshotCellEntity
            {
                Latitude = bin.Latitude,
                Longitude = bin.Longitude,
                Count = bin.Count
            }).ToArray()
        };
    }
}
