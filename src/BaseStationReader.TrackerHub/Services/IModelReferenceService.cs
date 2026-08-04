#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IModelReferenceService
{
    /// <summary>Lists manufacturers available for model editing.</summary>
    Task<List<Manufacturer>> ListManufacturersAsync(CancellationToken cancellationToken = default);
    /// <summary>Finds models matching the supplied codes, name, and manufacturer.</summary>
    Task<List<Model>> FindAsync(
        string? manufacturerName,
        string? modelName,
        string? modelIcao,
        CancellationToken cancellationToken = default);
    /// <summary>Adds or updates an aircraft model.</summary>
    Task<Model> SaveAsync(Model model, CancellationToken cancellationToken = default);
    /// <summary>Deletes an aircraft model.</summary>
    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
