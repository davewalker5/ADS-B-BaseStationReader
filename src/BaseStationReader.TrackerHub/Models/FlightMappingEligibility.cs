using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Defines whether a schedule row contains the fields required for database storage.
/// </summary>
public static class FlightMappingEligibility
{
    /// <summary>
    /// Returns whether a schedule row can be used by callsign-based flight lookup.
    /// </summary>
    public static bool IsEligible(FlightScheduleEntry mapping)
        => mapping is not null &&
           !string.IsNullOrWhiteSpace(mapping.FlightIATA) &&
           !string.IsNullOrWhiteSpace(mapping.Callsign) &&
           !string.IsNullOrWhiteSpace(mapping.AirlineIATA) &&
           !string.IsNullOrWhiteSpace(mapping.AirlineICAO);
}
