#nullable enable

using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

public interface IObservationSessionEditorService
{
    /// <summary>Gets an observation session for editing.</summary>
    Task<ObservationSessionDto?> GetAsync(int sessionId, CancellationToken cancellationToken = default);
    /// <summary>Saves the notes associated with an observation session.</summary>
    Task SaveNotesAsync(int sessionId, string? notes, CancellationToken cancellationToken = default);
    /// <summary>Deletes an observation session and its related data.</summary>
    Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default);
}
