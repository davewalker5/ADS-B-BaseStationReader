#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IManufacturerReferenceService
{
    /// <summary>Finds manufacturers matching the supplied name.</summary>
    Task<List<Manufacturer>> FindAsync(string? name, CancellationToken cancellationToken = default);
    /// <summary>Adds or updates a manufacturer.</summary>
    Task<Manufacturer> SaveAsync(Manufacturer manufacturer, CancellationToken cancellationToken = default);
    /// <summary>Deletes a manufacturer.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
