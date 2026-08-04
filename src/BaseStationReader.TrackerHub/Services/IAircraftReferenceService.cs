#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IAircraftReferenceService
{
    /// <summary>Finds aircraft matching the supplied address and registration filters.</summary>
    Task<List<Aircraft>> FindAsync(string? address, string? registration, CancellationToken cancellationToken = default);
    /// <summary>Lists the aircraft models available for editing.</summary>
    Task<List<Model>> ListModelsAsync(CancellationToken cancellationToken = default);
    /// <summary>Adds or updates an aircraft.</summary>
    Task<Aircraft> SaveAsync(Aircraft aircraft, CancellationToken cancellationToken = default);
    /// <summary>Deletes an aircraft.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
