namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Identifies an airport available for weather lookups.
/// </summary>
public sealed record AirportWeatherOption(string Name, string IATA, string ICAO)
{
    /// <summary>
    /// Gets the airport label displayed by the weather selector.
    /// </summary>
    public string DisplayName => $"{Name} ({IATA}/{ICAO})";
}
