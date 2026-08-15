using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>Provides aircraft-note operations to the Tracker Hub UI.</summary>
public interface IAircraftNoteService
{
    /// <summary>Adds a note for an aircraft.</summary>
    Task<AircraftNote> AddAsync(string address, string noteText, CancellationToken cancellationToken = default);

    /// <summary>Lists notes for an aircraft.</summary>
    Task<IReadOnlyList<AircraftNote>> ListAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>Deletes a note for an aircraft.</summary>
    Task DeleteAsync(int id, string address, CancellationToken cancellationToken = default);
}
