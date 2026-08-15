using BaseStationReader.Entities.Equipment;

namespace BaseStationReader.Interfaces.Database
{
    /// <summary>
    /// Manages the equipment associated with observation sessions.
    /// </summary>
    public interface ISessionEquipmentManager
    {
        /// <summary>Lists equipment associated with a session.</summary>
        Task<IReadOnlyList<SessionEquipment>> ListAsync(int sessionId, CancellationToken cancellationToken = default);
        /// <summary>Associates an equipment item with a session.</summary>
        Task<SessionEquipment> AddAsync(int sessionId, int equipmentId, CancellationToken cancellationToken = default);
        /// <summary>Removes an equipment association from a session.</summary>
        Task DeleteAsync(int sessionId, int equipmentId, CancellationToken cancellationToken = default);
    }
}
