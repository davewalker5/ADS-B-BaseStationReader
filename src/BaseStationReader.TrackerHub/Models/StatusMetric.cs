namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Represents one labelled integer on the operational status page.
/// </summary>
/// <param name="Label">The user-facing metric label.</param>
/// <param name="Value">The current metric value.</param>
public sealed record StatusMetric(string Label, int Value);
