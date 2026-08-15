#nullable enable

using BaseStationReader.Data;
using BaseStationReader.Entities.Equipment;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal sealed class EquipmentTypeManager : IEquipmentTypeManager
    {
        private readonly BaseStationReaderDbContext _context;

        public EquipmentTypeManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<IReadOnlyList<EquipmentType>> SearchAsync(
            string? name,
            CancellationToken cancellationToken = default)
        {
            var cleanName = name?.Trim().ToUpperInvariant() ?? "";
            IQueryable<EquipmentType> query = _context.EquipmentTypes.AsNoTracking();
            if (cleanName.Length > 0)
            {
                query = query.Where(equipmentType => equipmentType.Name.ToUpper().Contains(cleanName));
            }

            return await query.OrderBy(equipmentType => equipmentType.Name).ToListAsync(cancellationToken);
        }

        /// <inheritdoc />
        public async Task<EquipmentType> AddAsync(string name, CancellationToken cancellationToken = default)
        {
            var cleanName = CleanName(name);
            if (await _context.EquipmentTypes.AnyAsync(x => x.Name.ToUpper() == cleanName.ToUpper(), cancellationToken))
            {
                throw new InvalidOperationException($"An equipment type named '{cleanName}' already exists.");
            }

            var equipmentType = new EquipmentType { Name = cleanName };
            await _context.EquipmentTypes.AddAsync(equipmentType, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return equipmentType;
        }

        /// <inheritdoc />
        public async Task<EquipmentType> UpdateAsync(int id, string name, CancellationToken cancellationToken = default)
        {
            var equipmentType = await _context.EquipmentTypes.FindAsync([id], cancellationToken)
                ?? throw new InvalidOperationException($"Equipment type record {id} does not exist.");
            var cleanName = CleanName(name);
            if (await _context.EquipmentTypes.AnyAsync(
                x => x.Id != id && x.Name.ToUpper() == cleanName.ToUpper(),
                cancellationToken))
            {
                throw new InvalidOperationException($"An equipment type named '{cleanName}' already exists.");
            }

            equipmentType.Name = cleanName;
            await _context.SaveChangesAsync(cancellationToken);
            return equipmentType;
        }

        /// <inheritdoc />
        public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
        {
            var equipmentType = await _context.EquipmentTypes.FindAsync([id], cancellationToken)
                ?? throw new InvalidOperationException($"Equipment type record {id} does not exist.");
            if (await _context.Equipment.AnyAsync(x => x.EquipmentTypeId == id, cancellationToken))
            {
                throw new InvalidOperationException("The equipment type cannot be deleted while equipment uses it.");
            }

            _context.EquipmentTypes.Remove(equipmentType);
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string CleanName(string name)
        {
            ArgumentException.ThrowIfNullOrWhiteSpace(name);
            return name.Trim();
        }
    }
}
