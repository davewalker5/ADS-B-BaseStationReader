using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.History;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal sealed class ApiLogManager : IApiLogManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ApiLogManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <inheritdoc />
        public async Task<PagedResult<ApiLogEntry>> SearchAsync(
            ApiLogFilter filter,
            CancellationToken cancellationToken = default)
        {
            ArgumentNullException.ThrowIfNull(filter);
            if (filter.Page < 1)
            {
                throw new ArgumentOutOfRangeException(nameof(filter), "The page number must be at least one.");
            }
            if (filter.PageSize is < 1 or > 100)
            {
                throw new ArgumentOutOfRangeException(nameof(filter), "The page size must be between 1 and 100.");
            }
            if (filter.FromDate.HasValue && filter.ToDate.HasValue && filter.FromDate.Value.Date > filter.ToDate.Value.Date)
            {
                throw new ArgumentException("The from date must not be later than the to date.", nameof(filter));
            }

            IQueryable<ApiLogEntry> query = _context.ApiLogEntries.AsNoTracking();
            if (!string.IsNullOrWhiteSpace(filter.Service))
            {
                query = query.Where(entry => entry.Service == filter.Service);
            }
            if (!string.IsNullOrWhiteSpace(filter.Endpoint))
            {
                query = query.Where(entry => entry.Endpoint == filter.Endpoint);
            }
            if (filter.FromDate.HasValue)
            {
                query = query.Where(entry => entry.Timestamp >= filter.FromDate.Value.Date);
            }
            if (filter.ToDate.HasValue)
            {
                var exclusiveEnd = filter.ToDate.Value.Date.AddDays(1);
                query = query.Where(entry => entry.Timestamp < exclusiveEnd);
            }

            var totalItems = await query.CountAsync(cancellationToken);
            var totalPages = totalItems == 0 ? 0 : (int)Math.Ceiling(totalItems / (double)filter.PageSize);
            var page = totalPages == 0 ? 1 : Math.Min(filter.Page, totalPages);
            var items = await query
                .OrderByDescending(entry => entry.Timestamp)
                .ThenByDescending(entry => entry.Id)
                .Skip((page - 1) * filter.PageSize)
                .Take(filter.PageSize)
                .ToListAsync(cancellationToken);

            return new PagedResult<ApiLogEntry>
            {
                Items = items,
                Page = page,
                PageSize = filter.PageSize,
                TotalCount = totalItems
            };
        }

        /// <inheritdoc />
        public async Task<int> ClearAsync(CancellationToken cancellationToken = default)
        {
            var entries = await _context.ApiLogEntries.ToListAsync(cancellationToken);
            _context.ApiLogEntries.RemoveRange(entries);
            await _context.SaveChangesAsync(cancellationToken);
            return entries.Count;
        }

        /// <summary>
        /// Return all airlines matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<ApiLogEntry>> ListAsync(Expression<Func<ApiLogEntry, bool>> predicate)
            => await _context.ApiLogEntries.Where(predicate).ToListAsync();

        /// <summary>
        /// Add an airline, if it doesn't already exist
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<ApiLogEntry> AddAsync(
            ApiServiceType service,
            ApiEndpointType endpoint,
            string url,
            ApiProperty property,
            string propertyValue)
        {
            // No match, so create a new record
            var logEntry = new ApiLogEntry()
            {
                Service = service.ToString(),
                Endpoint = endpoint.ToString(),
                Url = url,
                Property = property.ToString(),
                PropertyValue = propertyValue,
                Timestamp = DateTime.Now
            };

            await _context.ApiLogEntries.AddAsync(logEntry);
            await _context.SaveChangesAsync();

            return logEntry;
        }
    }
}
