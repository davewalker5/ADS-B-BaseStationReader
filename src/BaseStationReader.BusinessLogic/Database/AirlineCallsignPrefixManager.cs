using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class AirlineCallsignPrefixManager : IAirlineCallsignPrefixManager
    {
        private readonly BaseStationReaderDbContext _context;

        public AirlineCallsignPrefixManager(BaseStationReaderDbContext context)
            => _context = context;

        /// <inheritdoc />
        public async Task<AirlineCallsignPrefix> GetAsync(
            Expression<Func<AirlineCallsignPrefix, bool>> predicate)
        {
            var mappings = await ListAsync(predicate);
            return mappings.FirstOrDefault();
        }

        /// <inheritdoc />
        public async Task<List<AirlineCallsignPrefix>> ListAsync(
            Expression<Func<AirlineCallsignPrefix, bool>> predicate)
            => await _context.AirlineCallsignPrefixes
                .Include(x => x.Airline)
                .Include(x => x.Provenance)
                .Where(predicate)
                .ToListAsync();

        /// <inheritdoc />
        public async Task<AirlineCallsignPrefix> AddAsync(
            string prefix,
            int airlineId,
            int provenanceId = 0)
        {
            var cleanPrefix = ValidatePrefix(prefix);
            await ValidateAirlineAsync(airlineId);
            provenanceId = await ResolveProvenanceIdAsync(provenanceId);

            var existing = await GetAsync(x => x.Prefix == cleanPrefix);
            if (existing != null)
            {
                if (existing.AirlineId == airlineId && existing.ProvenanceId == provenanceId)
                {
                    return existing;
                }

                throw new InvalidOperationException(
                    $"Callsign prefix '{cleanPrefix}' is already mapped to another airline or provenance record.");
            }

            var mapping = new AirlineCallsignPrefix
            {
                Prefix = cleanPrefix,
                AirlineId = airlineId,
                ProvenanceId = provenanceId
            };
            await _context.AirlineCallsignPrefixes.AddAsync(mapping);
            await _context.SaveChangesAsync();
            return await GetAsync(x => x.Id == mapping.Id);
        }

        /// <inheritdoc />
        public async Task<AirlineCallsignPrefix> UpdateAsync(
            int id,
            string prefix,
            int airlineId,
            int provenanceId)
        {
            var mapping = await _context.AirlineCallsignPrefixes.FindAsync(id)
                ?? throw new InvalidOperationException($"Airline callsign prefix record {id} does not exist.");
            var cleanPrefix = ValidatePrefix(prefix);
            await ValidateAirlineAsync(airlineId);
            await ValidateProvenanceAsync(provenanceId);

            if (await _context.AirlineCallsignPrefixes.AnyAsync(
                x => x.Id != id && x.Prefix == cleanPrefix))
            {
                throw new InvalidOperationException(
                    $"A mapping for callsign prefix '{cleanPrefix}' already exists.");
            }

            mapping.Prefix = cleanPrefix;
            mapping.AirlineId = airlineId;
            mapping.ProvenanceId = provenanceId;
            await _context.SaveChangesAsync();
            return await GetAsync(x => x.Id == id);
        }

        /// <inheritdoc />
        public async Task DeleteAsync(int id)
        {
            var mapping = await _context.AirlineCallsignPrefixes.FindAsync(id)
                ?? throw new InvalidOperationException($"Airline callsign prefix record {id} does not exist.");
            _context.AirlineCallsignPrefixes.Remove(mapping);
            await _context.SaveChangesAsync();
        }

        private async Task ValidateAirlineAsync(int airlineId)
        {
            if (!await _context.Airlines.AnyAsync(x => x.Id == airlineId))
            {
                throw new InvalidOperationException($"Airline record {airlineId} does not exist.");
            }
        }

        private async Task ValidateProvenanceAsync(int provenanceId)
        {
            if (!await _context.Provenance.AnyAsync(x => x.Id == provenanceId))
            {
                throw new InvalidOperationException($"Provenance record {provenanceId} does not exist.");
            }
        }

        private async Task<int> ResolveProvenanceIdAsync(int provenanceId)
        {
            if (provenanceId != 0)
            {
                await ValidateProvenanceAsync(provenanceId);
                return provenanceId;
            }

            var manual = await _context.Provenance.FirstOrDefaultAsync(x => x.SourceRef == "MANUAL");
            if (manual == null)
            {
                manual = new Provenance
                {
                    SourceRef = "MANUAL",
                    Source = "N/A",
                    SourceUrl = "N/A",
                    SourceDataset = "N/A",
                    SourceVersion = "N/A",
                    Licence = "N/A"
                };
                await _context.Provenance.AddAsync(manual);
                await _context.SaveChangesAsync();
            }

            return manual.Id;
        }

        private static string ValidatePrefix(string prefix)
        {
            var cleanPrefix = StringCleaner.CleanCallsignPrefix(prefix);
            if (cleanPrefix.Length is < 1 or > 8 || cleanPrefix.Any(c => c is not (>= 'A' and <= 'Z') and not (>= '0' and <= '9')))
            {
                throw new ArgumentException(
                    "Callsign prefix must contain between one and eight letters or digits.",
                    nameof(prefix));
            }

            return cleanPrefix;
        }
    }
}
