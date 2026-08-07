using System.Linq.Expressions;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.History;

namespace BaseStationReader.Interfaces.Database
{
    public interface IApiLogManager
    {
        /// <summary>
        /// Searches API log entries using the supplied filters and page settings.
        /// </summary>
        /// <param name="filter">The search and paging criteria.</param>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The matching page of log entries.</returns>
        Task<PagedResult<ApiLogEntry>> SearchAsync(
            ApiLogFilter filter,
            CancellationToken cancellationToken = default);

        /// <summary>
        /// Deletes every API log entry.
        /// </summary>
        /// <param name="cancellationToken">A token used to cancel the database operation.</param>
        /// <returns>The number of deleted entries.</returns>
        Task<int> ClearAsync(CancellationToken cancellationToken = default);

        Task<List<ApiLogEntry>> ListAsync(Expression<Func<ApiLogEntry, bool>> predicate);
        Task<ApiLogEntry> AddAsync(
            ApiServiceType service,
            ApiEndpointType endpoint,
            string url,
            ApiProperty property,
            string propertyValue);
    }
}
