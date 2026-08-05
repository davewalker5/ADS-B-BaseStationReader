using BaseStationReader.Entities.Config;

namespace BaseStationReader.Interfaces.Config;

/// <summary>
/// Builds spool-replayer configuration from a settings file and command-line overrides.
/// </summary>
public interface ISpoolReplayerSettingsBuilder
{
    /// <summary>
    /// Builds the effective application settings.
    /// </summary>
    /// <param name="parser">Parsed command line.</param>
    /// <param name="defaultConfigJsonPath">Default settings file.</param>
    /// <returns>Effective spool-replayer settings.</returns>
    SpoolReplayerApplicationSettings BuildSettings(
        ICommandLineParser parser,
        string defaultConfigJsonPath);
}
