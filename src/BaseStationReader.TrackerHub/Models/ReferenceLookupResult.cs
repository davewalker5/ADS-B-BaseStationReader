using BaseStationReader.Entities.Api;

#nullable enable

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Contains the aircraft and flight returned by an interactive lookup.
/// </summary>
/// <param name="Aircraft">The resolved aircraft, when available.</param>
/// <param name="Flight">The resolved flight, when available.</param>
public sealed record ReferenceLookupResult(Aircraft? Aircraft, Flight? Flight);
