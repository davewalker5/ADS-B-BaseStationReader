namespace BaseStationReader.Entities.Tracking;

/// <summary>
/// Represents one sampled point along a great-circle route.
/// </summary>
public sealed record RoutePointDto(double Latitude, double Longitude);
