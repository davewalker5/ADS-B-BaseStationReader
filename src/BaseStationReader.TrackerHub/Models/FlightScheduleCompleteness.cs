using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Models;

/// <summary>
/// Determines whether a flight schedule row contains the fields shown by the completeness indicator.
/// </summary>
public static class FlightScheduleCompleteness
{
    /// <summary>
    /// Returns whether a schedule row has complete flight and airline identification.
    /// </summary>
    /// <param name="entry">The schedule entry to assess.</param>
    /// <returns><see langword="true"/> when every completeness field is populated.</returns>
    public static bool IsComplete(FlightScheduleEntry entry)
        // The indicator describes identification quality only; it does not imply persistence eligibility.
        => entry is not null &&
           !string.IsNullOrWhiteSpace(entry.FlightIATA) &&
           !string.IsNullOrWhiteSpace(entry.Callsign) &&
           !string.IsNullOrWhiteSpace(entry.AirlineIATA) &&
           !string.IsNullOrWhiteSpace(entry.AirlineICAO);
}
