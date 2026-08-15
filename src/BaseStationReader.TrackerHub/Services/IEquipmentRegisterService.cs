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
}
