using BaseStationReader.Entities.History;

namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Prepares ordered position data and reusable summaries independently of rendering.
/// </summary>
public interface IFlightProfileBuilder
{
    /// <summary>
    /// Orders, numbers, enriches, and summarises a tracking record's position points.
    /// </summary>
    /// <param name="trackingRecordId">The owning tracking-record identifier.</param>
    /// <param name="address">The aircraft ICAO address.</param>
    /// <param name="callsign">The aircraft callsign, when known.</param>
    /// <param name="points">The raw persisted position projections.</param>
    /// <returns>A renderer-neutral flight profile.</returns>
    FlightProfileDto Build(
        int trackingRecordId,
        string address,
        string callsign,
        IEnumerable<FlightProfilePointDto> points);
}
