using BaseStationReader.Entities.Config;

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Contains the last submitted Weather-tab criteria for the current in-memory UI session.
/// </summary>
public sealed record WeatherPageState(
    ApiEndpointType Endpoint,
    ApiServiceType Service,
    string Icao);
