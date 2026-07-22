using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IProvenanceService
{
    Task<List<Provenance>> ListAsync(CancellationToken cancellationToken = default);
    Task<Provenance> SaveAsync(Provenance provenance, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
