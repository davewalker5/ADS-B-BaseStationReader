namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Contains a raw aviation weather report and its human-readable interpretation.
/// </summary>
public sealed record AirportWeatherReport(string Raw, IReadOnlyList<string> Decoded);
