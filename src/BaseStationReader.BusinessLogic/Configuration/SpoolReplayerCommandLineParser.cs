using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Config;

namespace BaseStationReader.BusinessLogic.Configuration;

/// <summary>
/// Defines the command-line contract for the spool replay utility.
/// </summary>
public sealed class SpoolReplayerCommandLineParser : CommandLineParser
{
    /// <summary>
    /// Initialises the parser and its supported options.
    /// </summary>
    /// <param name="generator">Command-line help generator.</param>
    public SpoolReplayerCommandLineParser(IHelpGenerator generator) : base(generator)
    {
        Add(CommandLineOptionType.Help, false, "--help", "-h", "Show command line help", 0, 0);
        Add(CommandLineOptionType.LogFile, false, "--log-file", "-l", "Log file path and name", 1, 1);
        Add(CommandLineOptionType.MinimumLogLevel, false, "--log-level", "-ll", "Minimum logging level", 1, 1);
        Add(CommandLineOptionType.SettingsFile, false, "--settings", "-s", "Specify an alternative application settings file", 1, 1);
        Add(CommandLineOptionType.SpoolFolder, false, "--spool-folder", "-sf", "Override the configured spool folder", 1, 1);
        Add(CommandLineOptionType.VerboseLogging, false, "--verbose", "-v", "Enable verbose logging at debug log level", 1, 1);
    }
}
