namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Represents one renderer-neutral point in a prepared geographic flight path.
/// </summary>
public sealed class FlightPathPointDto
{
    public int Sequence { get; init; }
    public int Segment { get; init; }
    public DateTime Timestamp { get; init; }
    public double Latitude { get; init; }
    public double Longitude { get; init; }
    public double AltitudeFeet { get; init; }
    public double AltitudeMetres { get; init; }
    public double DistanceNauticalMiles { get; init; }
    public double BearingDegrees { get; init; }
    public double LocalXMetres { get; init; }
    public double LocalYMetres { get; init; }
}
