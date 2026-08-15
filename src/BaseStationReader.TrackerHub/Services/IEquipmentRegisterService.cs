#nullable enable

using BaseStationReader.Entities.Equipment;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Provides equipment register operations to the Tracker Hub UI.
/// </summary>
public interface IEquipmentRegisterService
{
    Task<IReadOnlyList<EquipmentType>> SearchTypesAsync(string? name, CancellationToken cancellationToken = default);
    Task<EquipmentType> SaveTypeAsync(EquipmentType equipmentType, CancellationToken cancellationToken = default);
    Task DeleteTypeAsync(int id, CancellationToken cancellationToken = default);
    Task<IReadOnlyList<Equipment>> SearchEquipmentAsync(string? name, int? equipmentTypeId, CancellationToken cancellationToken = default);
    Task<Equipment> SaveEquipmentAsync(Equipment equipment, CancellationToken cancellationToken = default);
    Task DeleteEquipmentAsync(int id, CancellationToken cancellationToken = default);
    /// <summary>Lists equipment associated with a session.</summary>
    Task<IReadOnlyList<SessionEquipment>> ListSessionEquipmentAsync(int sessionId, CancellationToken cancellationToken = default);
    /// <summary>Associates an equipment item with a session.</summary>
    Task AddSessionEquipmentAsync(int sessionId, int equipmentId, CancellationToken cancellationToken = default);
    /// <summary>Removes an equipment association from a session.</summary>
    Task DeleteSessionEquipmentAsync(int sessionId, int equipmentId, CancellationToken cancellationToken = default);
}
