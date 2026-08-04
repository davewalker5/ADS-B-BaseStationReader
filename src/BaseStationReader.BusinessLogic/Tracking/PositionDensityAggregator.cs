using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Tracking;

/// <summary>
/// Aggregates geographic observations into the 45-column hexagonal grid used by position-density reporting.
/// </summary>
public sealed class PositionDensityAggregator : IPositionDensityAggregator
{
    private const int GridSize = 45;

    /// <summary>
    /// Builds renderer-neutral density bins for a single observation session.
    /// </summary>
    /// <param name="sessionId">The explicit session identifier represented by the coordinates.</param>
    /// <param name="coordinates">Valid persisted coordinates belonging only to the supplied session.</param>
    /// <param name="fixedBounds">Optional stable geographic bounds; live sessions supply these from persisted receiver settings.</param>
    /// <returns>Density bins and geographic bounds, or an empty model when there are no coordinates.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="sessionId"/> is not positive.</exception>
    public PositionDensity Aggregate(
        int sessionId,
        IReadOnlyCollection<PositionDensityCoordinate> coordinates,
        PositionDensityBounds? fixedBounds = null)
    {
        if (sessionId <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(sessionId));
        }
        var bounds = fixedBounds ?? BoundsFromCoordinates(coordinates);
        if (coordinates.Count == 0)
        {
            return EmptyDensity(sessionId, bounds);
        }

        var minimumLatitude = bounds.MinimumLatitude;
        var maximumLatitude = bounds.MaximumLatitude;
        var minimumLongitude = bounds.MinimumLongitude;
        var maximumLongitude = bounds.MaximumLongitude;

        // Expand a zero-width domain so repeated coordinates still occupy one stable, renderable bin.
        var latitudeSpan = Math.Max(maximumLatitude - minimumLatitude, 0.000001d);
        var longitudeSpan = Math.Max(maximumLongitude - minimumLongitude, 0.000001d);
        var rowCount = (int)Math.Round(GridSize / Math.Sqrt(3d));
        var counts = new Dictionary<(int Column, int Row), int>();

        foreach (var point in coordinates)
        {
            // Alternating row offsets approximate matplotlib's hexbin lattice while retaining geographic centres.
            var normalisedX = (point.Longitude - minimumLongitude) / longitudeSpan;
            var normalisedY = (point.Latitude - minimumLatitude) / latitudeSpan;
            var row = Math.Clamp((int)Math.Round(normalisedY * rowCount), 0, rowCount);
            var offset = row % 2 == 0 ? 0d : 0.5d;
            var column = Math.Clamp((int)Math.Round((normalisedX * GridSize) - offset), 0, GridSize);
            counts[(column, row)] = counts.GetValueOrDefault((column, row)) + 1;
        }

        var bins = counts
            .Select(item => new PositionDensityBin
            {
                Longitude = Math.Clamp(
                    minimumLongitude + ((item.Key.Column + (item.Key.Row % 2 == 0 ? 0d : 0.5d)) / GridSize * longitudeSpan),
                    minimumLongitude,
                    maximumLongitude),
                Latitude = minimumLatitude + ((double)item.Key.Row / rowCount * latitudeSpan),
                Count = item.Value
            })
            .OrderBy(bin => bin.Latitude)
            .ThenBy(bin => bin.Longitude)
            .ToArray();

        return new PositionDensity
        {
            SessionId = sessionId,
            PositionCount = coordinates.Count,
            MaximumBinCount = bins.Max(bin => bin.Count),
            MinimumLatitude = minimumLatitude,
            MaximumLatitude = maximumLatitude,
            MinimumLongitude = minimumLongitude,
            MaximumLongitude = maximumLongitude,
            Bins = bins
        };
    }

    /// <summary>
    /// Calculates bounds from observed coordinates for reporting callers that do not supply a stable viewport.
    /// </summary>
    /// <param name="coordinates">Coordinates included in the density calculation.</param>
    /// <returns>Observed bounds, or world bounds when no coordinates are available.</returns>
    private static PositionDensityBounds BoundsFromCoordinates(IReadOnlyCollection<PositionDensityCoordinate> coordinates)
    {
        // Live callers supply session bounds; this fallback preserves the reusable historical-reporting contract.
        return coordinates.Count == 0
            ? new PositionDensityBounds(-90d, 90d, -180d, 180d)
            : new PositionDensityBounds(
                coordinates.Min(point => point.Latitude),
                coordinates.Max(point => point.Latitude),
                coordinates.Min(point => point.Longitude),
                coordinates.Max(point => point.Longitude));
    }

    /// <summary>
    /// Creates an empty density result while retaining its stable rendering boundaries.
    /// </summary>
    /// <param name="sessionId">The represented session identifier.</param>
    /// <param name="bounds">Stable session boundaries.</param>
    /// <returns>An empty density model.</returns>
    private static PositionDensity EmptyDensity(int sessionId, PositionDensityBounds bounds)
    {
        // Retaining bounds means the first recorded position does not cause the chart viewport to change.
        return new PositionDensity
        {
            SessionId = sessionId,
            MinimumLatitude = bounds.MinimumLatitude,
            MaximumLatitude = bounds.MaximumLatitude,
            MinimumLongitude = bounds.MinimumLongitude,
            MaximumLongitude = bounds.MaximumLongitude
        };
    }
}
