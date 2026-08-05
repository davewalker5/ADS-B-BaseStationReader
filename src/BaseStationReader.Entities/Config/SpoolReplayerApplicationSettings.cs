using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Entities.Config;

/// <summary>
/// Contains configuration for the spool replay utility.
/// </summary>
[ExcludeFromCodeCoverage]
public sealed class SpoolReplayerApplicationSettings
{
    public string SettingsFile { get; set; } = "appsettings.json";

    public Severity MinimumLogLevel { get; set; }

    public string LogFile { get; set; } = "SpoolReplayer.log";

    public bool VerboseLogging { get; set; }

    public string SpoolFolder { get; set; } = "spool";

    public int TimeToLock { get; set; } = 900000;
}
