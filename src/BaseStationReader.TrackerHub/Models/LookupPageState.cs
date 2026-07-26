using BaseStationReader.Entities.Config;

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Contains the last submitted Lookup-tab criteria for the current in-memory UI session.
/// </summary>
public sealed record LookupPageState(
    ApiServiceType AircraftService,
    ApiServiceType FlightService,
    string Address,
    string Callsign);
