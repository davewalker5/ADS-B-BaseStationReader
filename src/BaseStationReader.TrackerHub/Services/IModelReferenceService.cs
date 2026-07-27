#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IModelReferenceService
{
    Task<List<Manufacturer>> ListManufacturersAsync(CancellationToken cancellationToken = default);
    Task<List<Model>> FindAsync(
        string? manufacturerName,
        string? modelName,
        string? modelIcao,
        CancellationToken cancellationToken = default);
    Task<Model> SaveAsync(Model model, CancellationToken cancellationToken = default);
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
