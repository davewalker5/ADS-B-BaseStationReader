#nullable enable

using BaseStationReader.Data;
using BaseStationReader.Entities.Equipment;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal sealed class EquipmentManager : IEquipmentManager
    {
        private readonly BaseStationReaderDbContext _context;

        public EquipmentManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<Equipment>> SearchAsync(
            string? name,
            int? equipmentTypeId = null,
            CancellationToken cancellationToken = default)
        {
            var cleanName = name?.Trim().ToUpperInvariant() ?? "";
            IQueryable<Equipment> query = _context.Equipment.AsNoTracking().Include(x => x.EquipmentType);
            if (cleanName.Length > 0)
            {
                query = query.Where(equipment => equipment.Name.ToUpper().Contains(cleanName));
            }
            if (equipmentTypeId.HasValue)
            {
                query = query.Where(equipment => equipment.EquipmentTypeId == equipmentTypeId.Value);
            }

            return await query.OrderBy(equipment => equipment.Name).ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Equipment> AddAsync(
            string name,
            int equipmentTypeId,
            CancellationToken cancellationToken = default)
        {
            var cleanName = CleanName(name);
            await ValidateTypeAsync(equipmentTypeId, cancellationToken);
            await ValidateUniqueNameAsync(cleanName, 0, cancellationToken);

            var equipment = new Equipment { Name = cleanName, EquipmentTypeId = equipmentTypeId };
            await _context.Equipment.AddAsync(equipment, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return await GetAsync(equipment.Id, cancellationToken);
        }

        /// <inheritdoc />
        public async Task<Equipment> UpdateAsync(
            int id,
            string name,
            int equipmentTypeId,
            CancellationToken cancellationToken = default)
        {
            var equipment = await _context.Equipment.FindAsync([id], cancellationToken)
                ?? throw new InvalidOperationException($"Equipment record {id} does not exist.");
            var cleanName = CleanName(name);
            await ValidateTypeAsync(equipmentTypeId, cancellationToken);
            await ValidateUniqueNameAsync(cleanName, id, cancellationToken);

            equipment.Name = cleanName;
            equipment.EquipmentTypeId = equipmentTypeId;
            await _context.SaveChangesAsync(cancellationToken);
            return await GetAsync(id, cancellationToken);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var equipment = await _context.Equipment.FindAsync([id], cancellationToken)
                ?? throw new InvalidOperationException($"Equipment record {id} does not exist.");
            if (await _context.SessionEquipment.AnyAsync(x => x.EquipmentId == id, cancellationToken))
            {
                throw new InvalidOperationException("The equipment cannot be deleted while it is associated with a session.");
            }
            _context.Equipment.Remove(equipment);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private async Task<Equipment> GetAsync(int id, CancellationToken cancellationToken)
            => await _context.Equipment.AsNoTracking().Include(x => x.EquipmentType)
                .SingleAsync(x => x.Id == id, cancellationToken);

        private async Task ValidateTypeAsync(int equipmentTypeId, CancellationToken cancellationToken)
        {
            if (!await _context.EquipmentTypes.AnyAsync(x => x.Id == equipmentTypeId, cancellationToken))
            {
                throw new InvalidOperationException($"Equipment type record {equipmentTypeId} does not exist.");
            }
        }

        private async Task ValidateUniqueNameAsync(string name, int excludedId, CancellationToken cancellationToken)
        {
            if (await _context.Equipment.AnyAsync(
                x => x.Id != excludedId && x.Name.ToUpper() == name.ToUpper(),
                cancellationToken))
            {
                throw new InvalidOperationException($"Equipment named '{name}' already exists.");
            }
        }

        private static string CleanName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return name.Trim();
        }
    }
}
