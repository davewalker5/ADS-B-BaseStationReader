using BaseStationReader.Entities.Api;
using System.Linq.Expressions;

namespace BaseStationReader.Interfaces.Database
{
    public interface IProvenanceManager
    {
        Task<Provenance> AddAsync(string sourceRef, string source, string sourceUrl,
            string sourceDataset, string sourceVersion, string licence);
        Task<Provenance> UpdateAsync(int id, string sourceRef, string source, string sourceUrl,
            string sourceDataset, string sourceVersion, string licence);
        Task DeleteAsync(int id);
        Task<Provenance> GetAsync(Expression<Func<Provenance, bool>> predicate);
        Task<List<Provenance>> ListAsync(Expression<Func<Provenance, bool>> predicate);
    }
}
