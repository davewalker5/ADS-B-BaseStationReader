#nullable enable

using BaseStationReader.Entities.Api;

namespace BaseStationReader.TrackerHub.Services;

public interface IAirlineCallsignPrefixReferenceService
{
    Task<List<AirlineCallsignPrefix>> FindAsync(
        string? prefix,
        string? airlineIcao,
        string? airlineName,
        CancellationToken cancellationToken = default);

    Task<AirlineCallsignPrefix> SaveAsync(
        AirlineCallsignPrefix mapping,
        CancellationToken cancellationToken = default);

    Task DeleteAsync(int id, CancellationToken cancellationToken = default);
}
