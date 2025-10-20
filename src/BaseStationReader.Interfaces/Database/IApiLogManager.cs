using System.Linq.Expressions;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;

namespace BaseStationReader.Interfaces.Database
{
    public interface IApiLogManager
    {
        Task<List<ApiLogEntry>> ListAsync(Expression<Func<ApiLogEntry, bool>> predicate);
        Task<ApiLogEntry> AddAsync(
            ApiServiceType service,
            ApiEndpointType endpoint,
            string url,
            ApiProperty property,
            string propertyValue);
    }
}