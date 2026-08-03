namespace BaseStationReader.Entities.History;

/// <summary>
/// Contains renderer-neutral position-density data for one observation session.
/// </summary>
public sealed class PositionDensityDto
{
    public int SessionId { get; init; }
    public int PositionCount { get; init; }
    public int MaximumBinCount { get; init; }
    public double MinimumLatitude { get; init; }
    public double MaximumLatitude { get; init; }
    public double MinimumLongitude { get; init; }
    public double MaximumLongitude { get; init; }
    public IReadOnlyList<PositionDensityBinDto> Bins { get; init; } = [];
}

/// <summary>
/// Describes one occupied geographic hexagonal bin.
/// </summary>
public sealed class PositionDensityBinDto
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int Count { get; init; }
}

/// <summary>
/// Supplies one valid geographic coordinate to the density aggregator.
/// </summary>
public readonly record struct PositionDensityCoordinate(double Latitude, double Longitude);

/// <summary>
/// Defines stable geographic boundaries used to bin and render one session's positions.
/// </summary>
public readonly record struct PositionDensityBounds(
    double MinimumLatitude,
    double MaximumLatitude,
    double MinimumLongitude,
    double MaximumLongitude);
