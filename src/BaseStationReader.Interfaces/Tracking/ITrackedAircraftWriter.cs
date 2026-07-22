using BaseStationReader.Entities.Tracking;
using System.Linq.Expressions;

namespace BaseStationReader.Interfaces.Tracking
{
    public interface ITrackedAircraftWriter
    {
        Task<TrackedAircraft> GetAsync(Expression<Func<TrackedAircraft, bool>> predicate);
        Task<TrackedAircraft> GetSightingCreationCandidateAsync(string address);
        Task<List<TrackedAircraft>> ListAsync(Expression<Func<TrackedAircraft, bool>> predicate);
        Task<List<TrackedAircraft>> ListSightingCreationCandidatesAsync();
        Task<TrackedAircraft> WriteAsync(TrackedAircraft template);
    }
}
