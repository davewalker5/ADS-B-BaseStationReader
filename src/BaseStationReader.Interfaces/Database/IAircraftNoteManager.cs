using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Database;

/// <summary>
/// Manages notes associated with aircraft ICAO addresses.
/// </summary>
public interface IAircraftNoteManager
{
    /// <summary>Adds a note using the current date and time.</summary>
    /// <param name="address">The six-character ICAO address.</param>
    /// <param name="noteText">The note text.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The saved note.</returns>
    Task<AircraftNote> AddAsync(string address, string noteText, CancellationToken cancellationToken = default);

    /// <summary>Lists notes for an ICAO address, newest first.</summary>
    /// <param name="address">The six-character ICAO address.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching notes.</returns>
    Task<IReadOnlyList<AircraftNote>> ListAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>Deletes a note belonging to an ICAO address.</summary>
    /// <param name="id">The note identifier.</param>
    /// <param name="address">The owning aircraft address.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task DeleteAsync(int id, string address, CancellationToken cancellationToken = default);
}
