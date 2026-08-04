namespace BaseStationReader.Entities.History;

/// <summary>
/// Supplies one valid geographic coordinate to the density aggregator.
/// </summary>
public readonly record struct PositionDensityCoordinate(double Latitude, double Longitude);
