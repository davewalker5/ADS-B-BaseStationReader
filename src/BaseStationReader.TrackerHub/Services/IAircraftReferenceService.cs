#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IAircraftReferenceService
{
    Task<List<Aircraft>> FindAsync(string? address, string? registration, CancellationToken cancellationToken = default);
    Task<List<Model>> ListModelsAsync(CancellationToken cancellationToken = default);
    Task<Aircraft> SaveAsync(Aircraft aircraft, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
