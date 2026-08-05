using Microsoft.Data.Sqlite;

#nullable enable

namespace BaseStationReader.BusinessLogic.Spool;

/// <summary>
/// Resolves the writer spool location relative to the configured SQLite database.
/// </summary>
public static class SpoolFolderResolver
{
    /// <summary>
    /// Resolves the configured spool directory.
    /// </summary>
    /// <param name="connectionString">SQLite database connection string.</param>
    /// <param name="spoolFolder">Configured absolute path or database-relative directory.</param>
    /// <returns>The absolute spool directory path.</returns>
    public static string Resolve(string connectionString, string? spoolFolder)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);

        var builder = new SqliteConnectionStringBuilder(connectionString);
        if (string.IsNullOrWhiteSpace(builder.DataSource) || builder.DataSource == ":memory:")
        {
            throw new ArgumentException("The spool requires a file-backed SQLite database.", nameof(connectionString));
        }

        var databasePath = Path.GetFullPath(builder.DataSource);
        var databaseFolder = Path.GetDirectoryName(databasePath)
            ?? throw new ArgumentException("The database directory could not be resolved.", nameof(connectionString));
        var configuredFolder = string.IsNullOrWhiteSpace(spoolFolder) ? "spool" : spoolFolder.Trim();

        return Path.IsPathFullyQualified(configuredFolder)
            ? Path.GetFullPath(configuredFolder)
            : Path.GetFullPath(configuredFolder, databaseFolder);
    }
}
