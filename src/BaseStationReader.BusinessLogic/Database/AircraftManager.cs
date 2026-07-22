using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class AircraftManager : IAircraftManager
    {
        private readonly BaseStationReaderDbContext _context;

        public AircraftManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first set of details matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<Aircraft> GetAsync(Expression<Func<Aircraft, bool>> predicate)
        {
            List<Aircraft> details = await ListAsync(predicate);
            return details.FirstOrDefault();
        }

        /// <summary>
        /// Return all details matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<Aircraft>> ListAsync(Expression<Func<Aircraft, bool>> predicate)
            => await _context.Aircraft
                .Where(predicate)
                .Include(x => x.Model)
                .ThenInclude(x => x.Manufacturer)
                .Include(x => x.Provenance)
                .ToListAsync();

        /// <summary>
        /// Add an aircraft, if the associated ICAO address doesn't already exist
        /// </summary>
        /// <param name="address"></param>
        /// <param name="registration"></param>
        /// <param name="manufactured"></param>
        /// <param name="age"></param>
        /// <param name="modelId"></param>
        /// <returns></returns>
        public async Task<Aircraft> AddAsync(string address, string registration, int? manufactured, int? age, int modelId, int provenanceId = 0)
        {
            if (provenanceId == 0)
            {
                var local = await _context.Provenance.FirstOrDefaultAsync(x => x.SourceRef == "LOCAL");
                if (local == null)
                {
                    local = new Provenance
                    {
                        SourceRef = "LOCAL", Source = "N/A", SourceUrl = "N/A",
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

            var aircraft = await GetAsync(a => a.Address == address);

            if (aircraft == null)
            {
                // Create a new instance
                aircraft = new Aircraft
                {
                    Address = address,
                    Registration = registration,
                    Manufactured = manufactured,
                    Age = age,
                    ModelId = modelId,
                    ProvenanceId = provenanceId
                };

                // Save the aircraft
                await _context.Aircraft.AddAsync(aircraft);
                await _context.SaveChangesAsync();

                // Load related entities
                await _context.Entry(aircraft).Reference(x => x.Model).LoadAsync();
                await _context.Entry(aircraft.Model).Reference(x => x.Manufacturer).LoadAsync();
                await _context.Entry(aircraft).Reference(x => x.Provenance).LoadAsync();
            }

            return aircraft;
        }
    }
}
