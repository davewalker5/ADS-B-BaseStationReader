namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Controls the bounded live-radar range and trail history.
/// </summary>
public sealed class RadarOptions
{
    public double MaximumRange { get; set; } = 50;
    public int TrailSeconds { get; set; } = 120;
    public int MaximumTrailPoints { get; set; } = 100;
    public int MaximumTrailAircraft { get; set; } = 250;
}
