namespace BaseStationReader.Entities.Tracking;

/// <summary>
/// Identifies one airport used as an endpoint of a plotted route.
/// </summary>
public sealed record RouteAirport(
    string Name,
    string Iata,
    double Latitude,
    double Longitude);
