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
    public async Task<ObservationSessionDto?> GetAsync(
        int sessionId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await context.ObservationSessions
            .AsNoTracking()
            .Where(session => session.Id == sessionId)
            .Select(session => new ObservationSessionDto
            {
                SessionId = session.Id,
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
            })
            .SingleOrDefaultAsync(cancellationToken);
    }

    public async Task SaveNotesAsync(
        int sessionId,
        string? notes,
        CancellationToken cancellationToken = default)
    {
        var normalisedNotes = string.IsNullOrWhiteSpace(notes) ? null : notes.Trim();
        if (normalisedNotes?.Length > 4000)
            throw new ArgumentException("Session notes cannot exceed 4,000 characters.", nameof(notes));

        // The runtime gate prevents a tracking session from starting until this update completes.
        await runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var session = await context.ObservationSessions
                .SingleOrDefaultAsync(item => item.Id == sessionId, cancellationToken)
                ?? throw new InvalidOperationException("The selected session could not be found.");

            session.Notes = normalisedNotes;
            await context.SaveChangesAsync(cancellationToken);
        }, cancellationToken);
    }

    public Task DeleteAsync(int sessionId, CancellationToken cancellationToken = default)
        => runtime.ExecuteWhileIdleAsync(async () =>
        {
            await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
            var manager = new DatabaseManagementFactory(logger, context, 0).ObservationSessionManager;
            await manager.DeleteAsync(sessionId, cancellationToken);
        }, cancellationToken);
}
