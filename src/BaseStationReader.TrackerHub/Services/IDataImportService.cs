namespace BaseStationReader.TrackerHub.Services;

/// <summary>
/// Identifies a CSV data set supported by the lookup tool importers.
/// </summary>
public enum DataImportType
{
    Aircraft,
    Airlines,
    FlightMappings,
    Manufacturers,
    Models
}

/// <summary>
/// Imports lookup data uploaded through the unified web UI.
/// </summary>
public interface IDataImportService
{
    /// <summary>
    /// Imports one uploaded CSV using the same importer as the lookup command-line tool.
    /// </summary>
    Task ImportAsync(
        DataImportType importType,
        Stream csvStream,
        string fileName,
        CancellationToken cancellationToken = default);
}
