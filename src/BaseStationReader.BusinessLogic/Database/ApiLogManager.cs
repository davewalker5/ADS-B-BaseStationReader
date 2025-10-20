using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class ApiLogManager : IApiLogManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ApiLogManager(BaseStationReaderDbContext context)
        {
            _context = context;
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