#nullable enable

using BaseStationReader.TrackerHub.Models;

namespace BaseStationReader.TrackerHub.Services;

public interface IObservationSessionEditorService
{
    /// <summary>Gets an observation session for editing.</summary>
    Task<ObservationSessionDto?> GetAsync(int sessionId, CancellationToken cancellationToken = default);
    /// <summary>Saves the editable name and notes associated with an observation session.</summary>
    Task SaveAsync(int sessionId, string name, string? notes, CancellationToken cancellationToken = default);
    /// <summary>Associates equipment with an observation session while tracking is idle.</summary>
    Task AddEquipmentAsync(int sessionId, int equipmentId, CancellationToken cancellationToken = default);
    /// <summary>Removes equipment from an observation session while tracking is idle.</summary>
    Task DeleteEquipmentAsync(int sessionId, int equipmentId, CancellationToken cancellationToken = default);
    /// <summary>Deletes an observation session and its related data.</summary>
    Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default);
}
