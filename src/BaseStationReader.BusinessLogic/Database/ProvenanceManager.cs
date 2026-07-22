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
            sourceRef = sourceRef?.Trim() ?? "";
            source = source?.Trim() ?? "";
            sourceUrl = sourceUrl?.Trim() ?? "";
            sourceDataset = sourceDataset?.Trim() ?? "";
            sourceVersion = sourceVersion?.Trim() ?? "";
            licence = licence?.Trim() ?? "";

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
    }
}
