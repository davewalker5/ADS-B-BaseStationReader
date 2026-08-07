#nullable enable

using System.Linq.Expressions;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Database
{
    public interface IExcludedCallsignManager
    {
        /// <summary>
        /// Searches excluded flight callsigns.
        /// </summary>
        /// <param name="callsign">An optional full or partial callsign.</param>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The matching exclusions ordered by callsign.</returns>
        Task<IReadOnlyList<ExcludedCallsign>> SearchAsync(
            string? callsign,
            CancellationToken cancellationToken = default);

        Task<bool> IsExcludedAsync(string callsign);
        Task<List<ExcludedCallsign>> ListAsync(Expression<Func<ExcludedCallsign, bool>> predicate);
        Task<ExcludedCallsign> AddAsync(
            string callsign,
            CancellationToken cancellationToken = default);
        Task DeleteAsync(string callsign, CancellationToken cancellationToken = default);
    }
}
