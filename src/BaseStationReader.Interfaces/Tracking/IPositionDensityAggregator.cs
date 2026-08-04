using BaseStationReader.Entities.History;

namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Aggregates geographic observations into position-density bins.
/// </summary>
public interface IPositionDensityAggregator
{
    /// <summary>
    /// Builds renderer-neutral density bins for a single observation session.
    /// </summary>
    PositionDensityDto Aggregate(
        int sessionId,
        IReadOnlyCollection<PositionDensityCoordinate> coordinates,
        PositionDensityBounds? fixedBounds = null);
}
