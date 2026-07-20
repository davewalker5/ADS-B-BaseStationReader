namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Identifies the two mapping-exclusion lists.
/// </summary>
public enum ExclusionType
{
    AircraftAddress,
    Callsign
}

/// <summary>
/// A mapping exclusion displayed by the unified web UI.
/// </summary>
public sealed record ExclusionEntry(int Id, string Value);

/// <summary>
/// Manages aircraft-address and callsign mapping exclusions.
/// </summary>
public interface IExclusionManagementService
{
    Task<IReadOnlyList<ExclusionEntry>> ListAsync(
        ExclusionType type,
        CancellationToken cancellationToken = default);

    Task AddAsync(
        ExclusionType type,
        string value,
        CancellationToken cancellationToken = default);

    Task UpdateAsync(
        ExclusionType type,
        int id,
        string value,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(
        ExclusionType type,
        int id,
        CancellationToken cancellationToken = default);
}
