#nullable enable

using BaseStationReader.Entities.Equipment;

namespace BaseStationReader.Interfaces.Database
{
    /// <summary>
    /// Manages equipment records.
    /// </summary>
    public interface IEquipmentManager
    {
        Task<IReadOnlyList<Equipment>> SearchAsync(string? name, int? equipmentTypeId = null, CancellationToken cancellationToken = default);
        Task<Equipment> AddAsync(string name, int equipmentTypeId, CancellationToken cancellationToken = default);
        Task<Equipment> UpdateAsync(int id, string name, int equipmentTypeId, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, CancellationToken cancellationToken = default);
    }
}
