using BaseStationReader.Entities.Tracking;
using System.Linq.Expressions;

namespace BaseStationReader.Interfaces.Database
{
    public interface IPositionWriter
    {
        Task<AircraftPosition> GetAsync(Expression<Func<AircraftPosition, bool>> predicate);
        Task<List<AircraftPosition>> ListAsync(Expression<Func<AircraftPosition, bool>> predicate);
        Task<AircraftPosition> WriteAsync(AircraftPosition template);
    }
}