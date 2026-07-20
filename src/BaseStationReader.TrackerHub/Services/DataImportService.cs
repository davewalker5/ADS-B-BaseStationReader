using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.BusinessLogic.Logging;
using BaseStationReader.Data;
using BaseStationReader.Interfaces.Logging;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Bridges browser uploads to the lookup tool's existing CSV importers.
/// </summary>
public sealed class DataImportService : IDataImportService
{
    private readonly IDbContextFactory<BaseStationReaderDbContext> _contextFactory;
    private readonly ITrackerLogger _logger;
    private readonly SemaphoreSlim _importLock = new(1, 1);

    public DataImportService(
        IDbContextFactory<BaseStationReaderDbContext> contextFactory,
        ITrackerLogger logger)
    {
        _contextFactory = contextFactory;
        _logger = logger;
    }

    /// <inheritdoc />
    public async Task ImportAsync(
        DataImportType importType,
        Stream csvStream,
        string fileName,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(csvStream);
        if (!csvStream.CanRead) throw new ArgumentException("The uploaded file cannot be read.", nameof(csvStream));
        if (!string.Equals(Path.GetExtension(fileName), ".csv", StringComparison.OrdinalIgnoreCase))
            throw new ArgumentException("Select a CSV file.", nameof(fileName));

        var temporaryPath = Path.Combine(Path.GetTempPath(), $"adsb-import-{Guid.NewGuid():N}.csv");
        try
        {
            await using (var temporaryFile = new FileStream(
                temporaryPath, FileMode.CreateNew, FileAccess.Write, FileShare.None, 81920, useAsync: true))
            {
                await csvStream.CopyToAsync(temporaryFile, cancellationToken);
            }

            // Imports are serialised because each importer performs a sequence of writes to the shared SQLite database.
            await _importLock.WaitAsync(cancellationToken);
            try
            {
                await using var context = await _contextFactory.CreateDbContextAsync(cancellationToken);
                var databaseFactory = new DatabaseManagementFactory(_logger, context, 0, 0);

                switch (importType)
                {
                    case DataImportType.Aircraft:
                        await new AircraftImporter(databaseFactory).ImportAsync(temporaryPath);
                        break;
                    case DataImportType.Airlines:
                        await new AirlineImporter(databaseFactory).ImportAsync(temporaryPath);
                        break;
                    case DataImportType.Airports:
                        await new AirportImporter(databaseFactory).ImportAsync(temporaryPath);
                        break;
                    case DataImportType.FlightMappings:
                        await new FlightIATACodeMappingImporter(databaseFactory).ImportAsync(temporaryPath);
                        break;
                    case DataImportType.Manufacturers:
                        await new ManufacturerImporter(databaseFactory).ImportAsync(temporaryPath);
                        break;
                    case DataImportType.Models:
                        await new ModelImporter(databaseFactory).ImportAsync(temporaryPath);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(importType));
                }
            }
            finally
            {
                _importLock.Release();
            }
        }
        finally
        {
            if (File.Exists(temporaryPath)) File.Delete(temporaryPath);
        }
    }
}
