#nullable enable

using System.Linq.Expressions;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Database
{
    public interface IExcludedAddressManager
    {
        /// <summary>
        /// Searches excluded aircraft addresses.
        /// </summary>
        /// <param name="address">An optional full or partial ICAO address.</param>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The matching exclusions ordered by address.</returns>
        Task<IReadOnlyList<ExcludedAddress>> SearchAsync(
            string? address,
            CancellationToken cancellationToken = default);

        Task<bool> IsExcludedAsync(string address);
        Task<List<ExcludedAddress>> ListAsync(Expression<Func<ExcludedAddress, bool>> predicate);
        Task<ExcludedAddress> AddAsync(string address);
        Task DeleteAsync(string address, CancellationToken cancellationToken = default);
    }
}
