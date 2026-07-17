#nullable enable

using BaseStationReader.Entities.Hub;
using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Converts live aircraft telemetry into receiver-centred radar coordinates.
/// </summary>
public interface IRadarProjectionService
{
    /// <summary>
    /// Projects an aircraft onto a normalised radar plane.
    /// </summary>
    /// <param name="aircraft">Latest live aircraft state.</param>
    /// <param name="maximumRange">Displayed range in nautical miles.</param>
    /// <returns>A radar point, or null when required position data is unavailable.</returns>
    RadarPointDto? Project(TrackedAircraftDto aircraft, double maximumRange);
}
