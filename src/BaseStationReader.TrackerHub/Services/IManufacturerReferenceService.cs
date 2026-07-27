#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IManufacturerReferenceService
{
    Task<List<Manufacturer>> FindAsync(string? name, CancellationToken cancellationToken = default);
    Task<Manufacturer> SaveAsync(Manufacturer manufacturer, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
