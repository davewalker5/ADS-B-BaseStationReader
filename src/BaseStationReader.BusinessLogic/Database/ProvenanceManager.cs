using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class ProvenanceManager : IProvenanceManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ProvenanceManager(BaseStationReaderDbContext context)
            => _context = context;

        public async Task<Provenance> GetAsync(Expression<Func<Provenance, bool>> predicate)
        {
            List<Provenance> records = await ListAsync(predicate);
            return records.FirstOrDefault();
        }

        public async Task<List<Provenance>> ListAsync(Expression<Func<Provenance, bool>> predicate)
            => await _context.Provenance.Where(predicate).ToListAsync();

        public async Task<Provenance> AddAsync(string sourceRef, string source, string sourceUrl,
            string sourceDataset, string sourceVersion, string licence)
        {
            Clean(ref sourceRef, ref source, ref sourceUrl, ref sourceDataset, ref sourceVersion, ref licence);

            var provenance = await GetAsync(x => x.SourceRef == sourceRef);
            if (provenance == null)
            {
                provenance = new Provenance
                {
                    SourceRef = sourceRef,
                    Source = source,
                    SourceUrl = sourceUrl,
                    SourceDataset = sourceDataset,
                    SourceVersion = sourceVersion,
                    Licence = licence
                };

                await _context.Provenance.AddAsync(provenance);
                await _context.SaveChangesAsync();
            }

            return provenance;
        }

        public async Task<Provenance> UpdateAsync(int id, string sourceRef, string source, string sourceUrl,
            string sourceDataset, string sourceVersion, string licence)
        {
            Clean(ref sourceRef, ref source, ref sourceUrl, ref sourceDataset, ref sourceVersion, ref licence);

            var provenance = await _context.Provenance.FirstOrDefaultAsync(x => x.Id == id)
                ?? throw new InvalidOperationException($"Provenance record {id} no longer exists.");

            if (await _context.Provenance.AnyAsync(x => x.Id != id && x.SourceRef == sourceRef))
                throw new InvalidOperationException($"A provenance record with source reference '{sourceRef}' already exists.");

            provenance.SourceRef = sourceRef;
            provenance.Source = source;
            provenance.SourceUrl = sourceUrl;
            provenance.SourceDataset = sourceDataset;
            provenance.SourceVersion = sourceVersion;
            provenance.Licence = licence;
            await _context.SaveChangesAsync();

            return provenance;
        }

        public async Task DeleteAsync(int id)
        {
            var provenance = await _context.Provenance.FirstOrDefaultAsync(x => x.Id == id);
            if (provenance != null)
            {
                _context.Provenance.Remove(provenance);
                await _context.SaveChangesAsync();
            }
        }

        private static void Clean(ref string sourceRef, ref string source, ref string sourceUrl,
            ref string sourceDataset, ref string sourceVersion, ref string licence)
        {
            sourceRef = sourceRef?.Trim() ?? "";
            source = source?.Trim() ?? "";
            sourceUrl = sourceUrl?.Trim() ?? "";
            sourceDataset = sourceDataset?.Trim() ?? "";
            sourceVersion = sourceVersion?.Trim() ?? "";
            licence = licence?.Trim() ?? "";
        }
    }
}
