using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Config;

namespace BaseStationReader.BusinessLogic.Configuration;

/// <summary>
/// Combines spool-replayer file settings with command-line overrides.
/// </summary>
public sealed class SpoolReplayerSettingsBuilder : ISpoolReplayerSettingsBuilder
{
    /// <inheritdoc />
    public SpoolReplayerApplicationSettings BuildSettings(
        ICommandLineParser parser,
        string defaultConfigJsonPath)
    {
        var values = parser.GetValues(CommandLineOptionType.SettingsFile);
        var configJsonPath = values is null ? defaultConfigJsonPath : values[0];
        var settings = new ConfigReader<SpoolReplayerApplicationSettings>().Read(configJsonPath);
        settings.SettingsFile = configJsonPath;

        values = parser.GetValues(CommandLineOptionType.LogFile);
        if (values is not null)
        {
            settings.LogFile = values[0];
        }

        values = parser.GetValues(CommandLineOptionType.MinimumLogLevel);
        if (values is not null && Enum.TryParse<Severity>(values[0], out var minimumLogLevel))
        {
            settings.MinimumLogLevel = minimumLogLevel;
        }

        values = parser.GetValues(CommandLineOptionType.SpoolFolder);
        if (values is not null)
        {
            settings.SpoolFolder = values[0];
        }

        values = parser.GetValues(CommandLineOptionType.VerboseLogging);
        if (values is not null)
        {
            settings.VerboseLogging = bool.Parse(values[0]);
        }

        return settings;
    }
}
