namespace BaseStationReader.Entities.Tracking;

/// <summary>
/// Summarises successful API reference results still held in the process-only cache.
/// </summary>
/// <param name="AircraftResolved">The number of distinct aircraft resolved by an API.</param>
/// <param name="FlightsResolved">The number of distinct flights resolved by an API.</param>
public sealed record TransientLookupStatus(int AircraftResolved, int FlightsResolved);
