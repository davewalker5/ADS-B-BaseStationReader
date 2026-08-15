#nullable enable

using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Database;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Creates short-lived database managers for equipment register UI operations.
/// </summary>
public sealed class EquipmentRegisterService(
    IDbContextFactory<BaseStationReaderDbContext> contextFactory,
    ITrackerLogger logger) : IEquipmentRegisterService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<EquipmentType>> SearchTypesAsync(
        string? name,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0).EquipmentTypeManager
            .SearchAsync(name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<EquipmentType> SaveTypeAsync(
        EquipmentType equipmentType,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(logger, context, 0).EquipmentTypeManager;
        return equipmentType.Id == 0
            ? await manager.AddAsync(equipmentType.Name, cancellationToken)
            : await manager.UpdateAsync(equipmentType.Id, equipmentType.Name, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteTypeAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await new DatabaseManagementFactory(logger, context, 0).EquipmentTypeManager.DeleteAsync(id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Equipment>> SearchEquipmentAsync(
        string? name,
        int? equipmentTypeId,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        return await new DatabaseManagementFactory(logger, context, 0).EquipmentManager
            .SearchAsync(name, equipmentTypeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Equipment> SaveEquipmentAsync(
        Equipment equipment,
        CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        var manager = new DatabaseManagementFactory(logger, context, 0).EquipmentManager;
        return equipment.Id == 0
            ? await manager.AddAsync(equipment.Name, equipment.EquipmentTypeId, cancellationToken)
            : await manager.UpdateAsync(equipment.Id, equipment.Name, equipment.EquipmentTypeId, cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteEquipmentAsync(int id, CancellationToken cancellationToken = default)
    {
        await using var context = await contextFactory.CreateDbContextAsync(cancellationToken);
        await new DatabaseManagementFactory(logger, context, 0).EquipmentManager.DeleteAsync(id, cancellationToken);
    }
}
