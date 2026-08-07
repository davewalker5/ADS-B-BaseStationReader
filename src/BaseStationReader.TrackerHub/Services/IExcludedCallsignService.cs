#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Provides excluded-callsign operations to the Tracker Hub UI.
/// </summary>
public interface IExcludedCallsignService
{
    /// <summary>
    /// Adds a callsign to the exclusion table.
    /// </summary>
    /// <param name="callsign">The callsign to exclude.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task AddAsync(string callsign, CancellationToken cancellationToken = default);

    /// <summary>
    /// Searches excluded flight callsigns.
    /// </summary>
    /// <param name="callsign">An optional full or partial callsign.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    /// <returns>The matching exclusions.</returns>
    Task<IReadOnlyList<ExcludedCallsign>> SearchAsync(
        string? callsign,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a callsign from the exclusion table.
    /// </summary>
    /// <param name="callsign">The callsign to remove.</param>
    /// <param name="cancellationToken">A token used to cancel the operation.</param>
    Task DeleteAsync(string callsign, CancellationToken cancellationToken = default);
}
