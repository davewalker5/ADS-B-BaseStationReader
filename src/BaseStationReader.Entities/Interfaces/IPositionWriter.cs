using BaseStationReader.Entities.Tracking;
using System.Linq.Expressions;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IPositionWriter
    {
        Task<AircraftPosition> GetAsync(Expression<Func<AircraftPosition, bool>> predicate);
        Task<List<AircraftPosition>> ListAsync(Expression<Func<AircraftPosition, bool>> predicate);
        Task<AircraftPosition> WriteAsync(AircraftPosition template);
    }
}