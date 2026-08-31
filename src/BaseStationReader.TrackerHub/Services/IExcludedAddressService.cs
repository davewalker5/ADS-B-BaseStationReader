#nullable enable

using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Provides excluded-address operations to the Tracker Hub UI.
/// </summary>
public interface IExcludedAddressService
{
    /// <summary>
    /// Adds an aircraft address to the exclusion table.
    /// </summary>
    /// <param name="address">The address to exclude.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches excluded aircraft addresses.
    /// </summary>
    /// <param name="address">An optional full or partial ICAO address.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching exclusions.</returns>
    Task<IReadOnlyList<ExcludedAddress>> SearchAsync(
        string? address,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes an aircraft address from the exclusion table.
    /// </summary>
    /// <param name="address">The address to remove.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task DeleteAsync(string address, CancellationToken cancellationToken = default);

    /// <summary>
    /// Deletes tracking data for every aircraft address in the exclusion table.
    /// </summary>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The number of tracked-aircraft records deleted.</returns>
    Task<int> PurgeTrackingDataAsync(CancellationToken cancellationToken = default);
}
