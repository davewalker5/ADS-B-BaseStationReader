namespace BaseStationReader.Entities.Spool;

/// <summary>
/// Identifies the entity carried by a persisted writer queue record.
/// </summary>
public enum SpoolEntityType
{
    TrackedAircraft,
    AircraftPosition,
    PositionDensitySnapshot
}
