namespace BaseStationReader.Entities.History;

/// <summary>
/// Defines stable geographic boundaries used to bin and render one session's positions.
/// </summary>
public readonly record struct PositionDensityBounds(
    double MinimumLatitude,
    double MaximumLatitude,
    double MinimumLongitude,
    double MaximumLongitude);
