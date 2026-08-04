namespace BaseStationReader.Entities.Tracking;

/// <summary>
/// Contains the airport endpoints and sampled geometry required by the route map.
/// </summary>
public sealed class RoutePlotDto
{
    public RouteAirportDto Origin { get; init; } = null!;
    public RouteAirportDto Destination { get; init; } = null!;
    public IReadOnlyList<RoutePointDto> Points { get; init; } = [];
    public double DistanceNauticalMiles { get; init; }
    public double CentreLatitude { get; init; }
    public double CentreLongitude { get; init; }
    public double LatitudeSpan { get; init; }
    public double LongitudeSpan { get; init; }
}
