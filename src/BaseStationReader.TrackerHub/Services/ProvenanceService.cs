using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

public sealed class ProvenanceService : IProvenanceService
{
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly ITrackerLogger _logger;

    public ProvenanceService(IDbContextFactory<BaseStationReaderDbContext> contextFactory, ITrackerLogger logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task<List<Provenance>> ListAsync(CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var factory = new DatabaseManagementFactory(_logger, context, 0);
        return await factory.ProvenanceManager.ListAsync(x => true);
    }

    /// <inheritdoc />
    public async Task<Provenance> SaveAsync(Provenance provenance, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(_logger, context, 0).ProvenanceManager;
        return provenance.Id == 0
            ? await manager.AddAsync(provenance.SourceRef, provenance.Source, provenance.SourceUrl,
                provenance.SourceDataset, provenance.SourceVersion, provenance.Licence)
            : await manager.UpdateAsync(provenance.Id, provenance.SourceRef, provenance.Source,
                provenance.SourceUrl, provenance.SourceDataset, provenance.SourceVersion, provenance.Licence);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        var factory = new DatabaseManagementFactory(_logger, context, 0);
        await factory.ProvenanceManager.DeleteAsync(id);
    }
}
