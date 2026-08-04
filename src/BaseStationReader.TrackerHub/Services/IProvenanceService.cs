using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IProvenanceService
{
    /// <summary>Lists provenance records.</summary>
    Task<List<Provenance>> ListAsync(CancellationToken cancellationToken = default);
    /// <summary>Adds or updates a provenance record.</summary>
    Task<Provenance> SaveAsync(Provenance provenance, CancellationToken cancellationToken = default);
    /// <summary>Deletes a provenance record.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
