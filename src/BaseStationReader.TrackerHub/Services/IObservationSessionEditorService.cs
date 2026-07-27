#nullable enable

using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

public interface IObservationSessionEditorService
{
    Task<ObservationSessionDto?> GetAsync(int sessionId, CancellationToken cancellationToken = default);
    Task SaveNotesAsync(int sessionId, string? notes, CancellationToken cancellationToken = default);
    Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default);
}
