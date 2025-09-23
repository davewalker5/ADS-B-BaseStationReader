using BaseStationReader.Entities.Tracking;
using System.Linq.Expressions;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAircraftWriter
    {
        Task<TrackedAircraft> GetAsync(Expression<Func<TrackedAircraft, bool>> predicate);
        Task<List<TrackedAircraft>> ListAsync(Expression<Func<TrackedAircraft, bool>> predicate);
        Task<TrackedAircraft> WriteAsync(TrackedAircraft template);
        Task<TrackedAircraft> SetLookupTimestamp(int id);
    }
}
