using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class ManufacturerManager : IManufacturerManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ManufacturerManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first manufacturer matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<Manufacturer> GetAsync(Expression<Func<Manufacturer, bool>> predicate)
        {
            List<Manufacturer> manufacturers = await ListAsync(predicate);
            return manufacturers.FirstOrDefault();
        }

        /// <summary>
        /// Return all manufacturers matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<Manufacturer>> ListAsync(Expression<Func<Manufacturer, bool>> predicate)
            => await _context.Manufacturers.Include(x => x.Provenance).Where(predicate).ToListAsync();

        /// <summary>
        /// Add a manufacturer, if it doesn't already exist
        /// </summary>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Manufacturer> AddAsync(string name, int provenanceId = 0)
        {
            // Clean the inputs so they're in a standardised format
            var clean = StringCleaner.CleanName(name);

            if (provenanceId == 0)
            {
                var local = await _context.Provenance.FirstOrDefaultAsync(x => x.SourceRef == "MANUAL");
                if (local == null)
                {
                    local = new Provenance
                    {
                        SourceRef = "MANUAL", Source = "N/A", SourceUrl = "N/A",
                        SourceDataset = "N/A", SourceVersion = "N/A", Licence = "N/A"
                    };
                    await _context.Provenance.AddAsync(local);
                    await _context.SaveChangesAsync();
                }
                provenanceId = local.Id;
            }
            else if (!await _context.Provenance.AnyAsync(x => x.Id == provenanceId))
            {
                throw new InvalidOperationException($"Provenance record {provenanceId} does not exist.");
            }

            // Look for a matching record
            var manufacturer = await GetAsync(a => a.Name == clean);

            if (manufacturer == null)
            {
                // No match, so create a new record
                manufacturer = new Manufacturer { Name = clean, ProvenanceId = provenanceId };

                await _context.Manufacturers.AddAsync(manufacturer);
                await _context.SaveChangesAsync();
            }

            return manufacturer;
        }

        /// <inheritdoc />
        public async Task<Manufacturer> UpdateAsync(int id, string name, int provenanceId)
        {
            var manufacturer = await _context.Manufacturers.FindAsync(id)
                ?? throw new InvalidOperationException($"Manufacturer record {id} does not exist.");
            if (!await _context.Provenance.AnyAsync(x => x.Id == provenanceId))
            {
                throw new InvalidOperationException($"Provenance record {provenanceId} does not exist.");
            }

            var cleanName = StringCleaner.CleanName(name);
            if (await _context.Manufacturers.AnyAsync(x => x.Id != id && x.Name == cleanName))
            {
                throw new InvalidOperationException($"A manufacturer named '{cleanName}' already exists.");
            }

            manufacturer.Name = cleanName;
            manufacturer.ProvenanceId = provenanceId;
            await _context.SaveChangesAsync();
            return await GetAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(int id)
        {
            var manufacturer = await _context.Manufacturers.FindAsync(id)
                ?? throw new InvalidOperationException($"Manufacturer record {id} does not exist.");
            _context.Manufacturers.Remove(manufacturer);
            await _context.SaveChangesAsync();
        }
    }
}
