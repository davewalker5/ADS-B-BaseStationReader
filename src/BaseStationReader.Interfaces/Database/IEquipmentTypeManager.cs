#nullable enable

using BaseStationReader.Entities.Equipment;

namespace BaseStationReader.Interfaces.Database
{
    /// <summary>
    /// Manages equipment type records.
    /// </summary>
    public interface IEquipmentTypeManager
    {
        Task<IReadOnlyList<EquipmentType>> SearchAsync(string? name, CancellationToken cancellationToken = default);
        Task<EquipmentType> AddAsync(string name, CancellationToken cancellationToken = default);
        Task<EquipmentType> UpdateAsync(int id, string name, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
