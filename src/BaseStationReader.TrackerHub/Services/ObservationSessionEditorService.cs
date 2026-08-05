#nullable enable

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.TrackerHub.Models;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

public sealed class ObservationSessionEditorService(
    IDbContextFactory<BaseStationReaderDbContext> contextFactory,
    TrackingRuntime runtime,
    ITrackerLogger logger) : IObservationSessionEditorService
{
    /// <inheritdoc />
    /// <inheritdoc />
    public async Task<ObservationSessionDto?> GetAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(logger, context, 0).ObservationSessionManager;

        // Retrieve the session through business logic before shaping it for the editor UI.
        var session = await manager.GetAsync(sessionId, cancellationToken);
        if (session is null)
        {
            return null;
        }
        return new ObservationSessionDto
        {
            SessionId = session.Id,
            Name = session.Name,
            StartedAtUtc = session.StartedAtUtc,
            ProfileName = session.ProfileName,
            Notes = session.Notes,
            Host = session.Host,
            Port = session.Port,
            ReceiverLatitude = session.ReceiverLatitude,
            ReceiverLongitude = session.ReceiverLongitude,
            ReceiverElevation = session.ReceiverElevation,
            MinimumAltitude = session.MinimumAltitude,
            MaximumAltitude = session.MaximumAltitude,
            MaximumDistance = session.MaximumDistance,
            IncludedBehaviours = session.IncludedBehaviours
        };
    }

    /// <inheritdoc />
    public async Task SaveAsync(
        int sessionId,
        string name,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        // The runtime gate prevents a tracking session from starting until this update completes.
        await runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var manager = new DatabaseManagementFactory(logger, context, 0).ObservationSessionManager;
            await manager.UpdateAsync(sessionId, name, notes, cancellationToken);
        }, cancellationToken);
    }

    /// <inheritdoc />
    public Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default)
        => runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var manager = new DatabaseManagementFactory(logger, context, 0).ObservationSessionManager;
            await manager.DeleteAsync(sessionId, cancellationToken);
        }, cancellationToken);
}
