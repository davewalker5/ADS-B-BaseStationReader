using System.Linq.Expressions;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Database
{
    public interface IExcludedAddressManager
    {
        Task<bool> IsExcludedAsync(string address);
        Task<List<ExcludedAddress>> ListAsync(Expression<Func<ExcludedAddress, bool>> predicate);
        Task<ExcludedAddress> AddAsync(string address);
        Task DeleteAsync(string address);
    }
}