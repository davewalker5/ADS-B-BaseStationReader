using BaseStationReader.Entities.Lookup;
using System.Linq.Expressions;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IManufacturerManager
    {
        Task<Manufacturer> AddAsync(string name);
        Task<Manufacturer> GetAsync(Expression<Func<Manufacturer, bool>> predicate);
        Task<List<Manufacturer>> ListAsync(Expression<Func<Manufacturer, bool>> predicate);
    }
}