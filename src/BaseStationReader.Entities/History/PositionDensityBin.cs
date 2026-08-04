namespace BaseStationReader.Entities.History;

/// <summary>
/// Describes one occupied geographic hexagonal bin.
/// </summary>
public sealed class PositionDensityBin
{
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public int Count { get; init; }
}