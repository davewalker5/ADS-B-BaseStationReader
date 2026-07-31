#nullable enable

using BaseStationReader.Data;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class ObservationSessionManager : IObservationSessionManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ObservationSessionManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<ObservationSession?> GetAsync(
            int sessionId,
            CancellationToken cancellationToken = default)
        {
            // Session reads are informational, so keep the manager's context free of tracked snapshots.
            return await _context.ObservationSessions
                .AsNoTracking()
                .SingleOrDefaultAsync(item => item.Id == sessionId, cancellationToken);
        }

        /// <summary>
        /// Add a new observation session
        /// </summary>
        /// <param name="session"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        public async Task AddAsync(
            ObservationSession session,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(session);

            await _context.ObservationSessions.AddAsync(session, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Update the properties of an observation session
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="notes"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task UpdateAsync(
            int sessionId,
            string? notes,
            CancellationToken cancellationToken = default)
        {
            var normalisedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
            if (normalisedNotes?.Length > 4000)
                throw new ArgumentException("Session notes cannot exceed 4,000 characters.", nameof(notes));

            var session = await _context.ObservationSessions
                .SingleOrDefaultAsync(item => item.Id == sessionId, cancellationToken)
                ?? throw new InvalidOperationException("The selected session could not be found.");

            session.Notes = normalisedNotes;
            await _context.SaveChangesAsync(cancellationToken);
        }

        /// <summary>
        /// Delete a session and related data given its ID
        /// </summary>
        /// <param name="sessionId"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        public async Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default)
        {
            var session = await _context.ObservationSessions
                .SingleOrDefaultAsync(item => item.Id == sessionId, cancellationToken)
                ?? throw new InvalidOperationException("The selected session could not be found.");

            var transaction = _context.Database.IsRelational()
                ? await _context.Database.BeginTransactionAsync(cancellationToken)
                : null;

            try
            {
                // Retrieve the associated tracked aircraft
                var trackedAircraft = await _context.TrackedAircraft
                    .Where(item => item.SessionId == sessionId)
                    .ToListAsync(cancellationToken);
                var trackedAircraftIds = trackedAircraft.Select(item => item.Id).ToList();

                // Retrieve the associated positions
                var positions = await _context.Positions
                    .Where(item => trackedAircraftIds.Contains(item.AircraftId))
                    .ToListAsync(cancellationToken);

                // Remove the data
                _context.Positions.RemoveRange(positions);
                _context.TrackedAircraft.RemoveRange(trackedAircraft);
                _context.ObservationSessions.Remove(session);
                await _context.SaveChangesAsync(cancellationToken);

                if (transaction is not null)
                    await transaction.CommitAsync(cancellationToken);
            }
            catch
            {
                if (transaction is not null)
                    await transaction.RollbackAsync(cancellationToken);
                throw;
            }
            finally
            {
                if (transaction is not null)
                    await transaction.DisposeAsync();
            }
        }
    }
}
