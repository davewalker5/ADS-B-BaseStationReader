using BaseStationReader.Entities.Api;
using System.Linq.Expressions;

namespace BaseStationReader.Interfaces.Database
{
    public interface IProvenanceManager
    {
        Task<Provenance> AddAsync(string sourceRef, string source, string sourceUrl,
            string sourceDataset, string sourceVersion, string licence);
        Task<Provenance> GetAsync(Expression<Func<Provenance, bool>> predicate);
        Task<List<Provenance>> ListAsync(Expression<Func<Provenance, bool>> predicate);
    }
}
