using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Prepares persisted positions for renderer-neutral geographic and 3D plotting.
/// </summary>
public interface IFlightPathBuilder
{
    /// <summary>
    /// Validates, de-duplicates, orders, segments, projects, and summarises a flight path.
    /// </summary>
    /// <param name="trackingRecordId">Owning tracking-record identifier.</param>
    /// <param name="address">Aircraft ICAO address.</param>
    /// <param name="callsign">Aircraft callsign.</param>
    /// <param name="registration">Aircraft registration.</param>
    /// <param name="model">Aircraft model.</param>
    /// <param name="flightNumber">Associated flight number.</param>
    /// <param name="airline">Associated airline.</param>
    /// <param name="route">Associated route.</param>
    /// <param name="points">Raw persisted positions.</param>
    /// <returns>A renderer-neutral flight path.</returns>
    FlightPathDto Build(
        int trackingRecordId,
        string address,
        string callsign,
        string registration,
        string model,
        string flightNumber,
        string airline,
        string route,
        IEnumerable<FlightProfilePointDto> points);
}
