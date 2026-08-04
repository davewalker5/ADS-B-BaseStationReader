using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Tracking;

/// <summary>
/// Provides aggregate status for successful reference lookups retained transiently in memory.
/// </summary>
public interface ITransientReferenceStatusProvider
{
    /// <summary>
    /// Summarises successful reference lookups that remain available transiently.
    /// </summary>
    /// <returns>Counts of distinct transient aircraft and flight results.</returns>
    TransientLookupStatus GetReferenceLookupStatus();
}
