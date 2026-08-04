namespace BaseStationReader.Entities.History;

/// <summary>
/// Contains renderer-neutral position-density data for one observation session.
/// </summary>
public sealed class PositionDensity
{
    public int SessionId { get; init; }
    public int PositionCount { get; init; }
    public int MaximumBinCount { get; init; }
    public double MinimumLatitude { get; init; }
    public double MaximumLatitude { get; init; }
    public double MinimumLongitude { get; init; }
    public double MaximumLongitude { get; init; }
    public IReadOnlyList<PositionDensityBin> Bins { get; init; } = [];
}
