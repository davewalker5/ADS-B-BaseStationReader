#nullable enable

using BaseStationReader.Entities.History;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Coordinates periodic in-memory position-density updates for one tracking session.
/// </summary>
public interface IPositionDensitySnapshotOrchestrator
{
    /// <summary>
    /// Starts periodic snapshot updates for a session.
    /// </summary>
    /// <param name="sessionId"></param>
    /// <param name="bounds"></param>
    /// <param name="interval"></param>
    /// <param name="cancellationToken"></param>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    void Start(int sessionId, PositionDensityBounds bounds, TimeSpan interval, CancellationToken cancellationToken);

    /// <summary>
    /// Adds one valid observed position to the current snapshot input.
    /// </summary>
    /// <param name="position"></param>
    void Record(AircraftPosition? position);

    /// <summary>
    /// Stops periodic snapshot updates and releases session input data.
    /// </summary>
    /// <returns></returns>
    Task StopAsync();
}
