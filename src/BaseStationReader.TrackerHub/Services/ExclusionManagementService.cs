using BaseStationReader.Data;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Provides validated CRUD access to the two exclusion tables.
/// </summary>
public sealed class ExclusionManagementService : IExclusionManagementService
{
    private const int MaximumCallsignLength = 32;
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;

    public ExclusionManagementService(IDbContextFactory<BaseStationReaderDbContext> contextFactory)
        => _contextFactory = contextFactory;

    /// <inheritdoc />
    public async Task<IReadOnlyList<ExclusionEntry>> ListAsync(
        ExclusionType type,
        CancellationToken cancellationToken = default)
    {
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
        return type switch
        {
            ExclusionType.AircraftAddress => await context.ExcludedAddresses
                .AsNoTracking()
                .OrderBy(exclusion => exclusion.Address)
                .Select(exclusion => new ExclusionEntry(exclusion.Id, exclusion.Address))
                .ToListAsync(cancellationToken),
            ExclusionType.Callsign => await context.ExcludedCallsigns
                .AsNoTracking()
                .OrderBy(exclusion => exclusion.Callsign)
                .Select(exclusion => new ExclusionEntry(exclusion.Id, exclusion.Callsign))
                .ToListAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };
    }

    /// <inheritdoc />
    public async Task AddAsync(
        ExclusionType type,
        string value,
        CancellationToken cancellationToken = default)
    {
        var normalised = ValidateAndNormalise(type, value);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        switch (type)
        {
            case ExclusionType.AircraftAddress:
                if (await context.ExcludedAddresses.AnyAsync(x => x.Address == normalised, cancellationToken))
                    throw new InvalidOperationException($"Aircraft address {normalised} is already excluded.");
                await context.ExcludedAddresses.AddAsync(new() { Address = normalised }, cancellationToken);
                break;
            case ExclusionType.Callsign:
                if (await context.ExcludedCallsigns.AnyAsync(x => x.Callsign == normalised, cancellationToken))
                    throw new InvalidOperationException($"Callsign {normalised} is already excluded.");
                await context.ExcludedCallsigns.AddAsync(new() { Callsign = normalised }, cancellationToken);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task UpdateAsync(
        ExclusionType type,
        int id,
        string value,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        var normalised = ValidateAndNormalise(type, value);
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        switch (type)
        {
            case ExclusionType.AircraftAddress:
            {
                var exclusion = await context.ExcludedAddresses.FindAsync([id], cancellationToken)
                    ?? throw new InvalidOperationException("The aircraft-address exclusion no longer exists.");
                if (await context.ExcludedAddresses.AnyAsync(x => x.Id != id && x.Address == normalised, cancellationToken))
                    throw new InvalidOperationException($"Aircraft address {normalised} is already excluded.");
                exclusion.Address = normalised;
                break;
            }
            case ExclusionType.Callsign:
            {
                var exclusion = await context.ExcludedCallsigns.FindAsync([id], cancellationToken)
                    ?? throw new InvalidOperationException("The callsign exclusion no longer exists.");
                if (await context.ExcludedCallsigns.AnyAsync(x => x.Id != id && x.Callsign == normalised, cancellationToken))
                    throw new InvalidOperationException($"Callsign {normalised} is already excluded.");
                exclusion.Callsign = normalised;
                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(type));
        }

        await context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task DeleteAsync(
        ExclusionType type,
        int id,
        CancellationToken cancellationToken = default)
    {
        if (id <= 0) throw new ArgumentOutOfRangeException(nameof(id));
        await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);

        var removed = type switch
        {
            ExclusionType.AircraftAddress => await context.ExcludedAddresses
                .Where(exclusion => exclusion.Id == id)
                .ExecuteDeleteAsync(cancellationToken),
            ExclusionType.Callsign => await context.ExcludedCallsigns
                .Where(exclusion => exclusion.Id == id)
                .ExecuteDeleteAsync(cancellationToken),
            _ => throw new ArgumentOutOfRangeException(nameof(type))
        };

        if (removed == 0) throw new InvalidOperationException("The exclusion no longer exists.");
    }

    private static string ValidateAndNormalise(ExclusionType type, string value)
    {
        var normalised = (value ?? string.Empty).Trim().ToUpperInvariant();
        if (type == ExclusionType.AircraftAddress)
        {
            if (normalised.Length != 6 || !normalised.All(Uri.IsHexDigit))
                throw new ArgumentException("Enter a six-character hexadecimal aircraft address.", nameof(value));
        }
        else if (type == ExclusionType.Callsign)
        {
            if (normalised.Length == 0 || normalised.Length > MaximumCallsignLength)
                throw new ArgumentException($"Enter a callsign between 1 and {MaximumCallsignLength} characters.", nameof(value));
            if (normalised.Any(char.IsWhiteSpace))
                throw new ArgumentException("A callsign cannot contain spaces.", nameof(value));
        }
        else
        {
            throw new ArgumentOutOfRangeException(nameof(type));
        }

        return normalised;
    }
}
