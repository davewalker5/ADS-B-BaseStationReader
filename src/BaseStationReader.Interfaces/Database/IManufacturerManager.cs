using BaseStationReader.Entities.Api;
using System.Linq.Expressions;

namespace BaseStationReader.Interfaces.Database
{
    public interface IManufacturerManager
    {
        Task<Manufacturer> AddAsync(string name, int provenanceId = 0);
        Task<Manufacturer> UpdateAsync(int id, string name, int provenanceId);
        Task DeleteAsync(int id);
        Task<Manufacturer> GetAsync(Expression<Func<Manufacturer, bool>> predicate);
        Task<List<Manufacturer>> ListAsync(Expression<Func<Manufacturer, bool>> predicate);
    }
}
