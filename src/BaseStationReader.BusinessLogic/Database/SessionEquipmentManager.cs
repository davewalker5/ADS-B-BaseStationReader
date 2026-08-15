using BaseStationReader.Data;
using BaseStationReader.Entities.Equipment;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal sealed class SessionEquipmentManager : ISessionEquipmentManager
    {
        private readonly BaseStationReaderDbContext _context;

        public SessionEquipmentManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<SessionEquipment>> ListAsync(
            int sessionId,
            CancellationToken cancellationToken = default)
            => await _context.SessionEquipment
                .AsNoTracking()
                .Include(x => x.Equipment)
                .ThenInclude(x => x.EquipmentType)
                .Where(x => x.SessionId == sessionId)
                .OrderBy(x => x.Equipment.EquipmentType.Name)
                .ThenBy(x => x.Equipment.Name)
                .ToListAsync(cancellationToken);

        /// <inheritdoc />
        public async Task<SessionEquipment> AddAsync(
            int sessionId,
            int equipmentId,
            CancellationToken cancellationToken = default)
        {
            if (!await _context.ObservationSessions.AnyAsync(x => x.Id == sessionId, cancellationToken))
            {
                throw new InvalidOperationException($"Session record {sessionId} does not exist.");
            }
            if (!await _context.Equipment.AnyAsync(x => x.Id == equipmentId, cancellationToken))
            {
                throw new InvalidOperationException($"Equipment record {equipmentId} does not exist.");
            }

            var existing = await _context.SessionEquipment
                .FirstOrDefaultAsync(x => x.SessionId == sessionId && x.EquipmentId == equipmentId, cancellationToken);
            if (existing is not null)
            {
                return existing;
            }

            var association = new SessionEquipment { SessionId = sessionId, EquipmentId = equipmentId };
            await _context.SessionEquipment.AddAsync(association, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return association;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(
            int sessionId,
            int equipmentId,
            CancellationToken cancellationToken = default)
        {
            var association = await _context.SessionEquipment.FirstOrDefaultAsync(
                x => x.SessionId == sessionId && x.EquipmentId == equipmentId,
                cancellationToken);
            if (association is null)
            {
                return;
            }

            _context.SessionEquipment.Remove(association);
            await _context.SaveChangesAsync(cancellationToken);
        }
    }
}
