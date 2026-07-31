#nullable enable

using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Database
{
    public interface IObservationSessionManager
    {
        /// <summary>
        /// Returns one observation session without tracking it for changes.
        /// </summary>
        /// <param name="sessionId">The session identifier.</param>
        /// <param name="cancellationToken">Cancels the database read.</param>
        /// <returns>The matching session, or <see langword="null"/> when it does not exist.</returns>
        Task<ObservationSession?> GetAsync(
            int sessionId,
            CancellationToken cancellationToken = default);

        Task AddAsync(ObservationSession session, CancellationToken cancellationToken = default);
        Task UpdateAsync(int sessionId, string? notes, CancellationToken cancellationToken = default);
        Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default);
    }
}
