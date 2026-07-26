namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Identifies one airport used as an endpoint of a plotted route.
/// </summary>
public sealed record RouteAirportDto(
    string Name,
    string Iata,
    double Latitude,
    double Longitude);
