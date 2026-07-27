#nullable enable

using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Database
{
    public interface IObservationSessionManager
    {
        Task AddAsync(ObservationSession session, CancellationToken cancellationToken = default);
        Task UpdateAsync(int sessionId, string? notes, CancellationToken cancellationToken = default);
        Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default);
    }
}
