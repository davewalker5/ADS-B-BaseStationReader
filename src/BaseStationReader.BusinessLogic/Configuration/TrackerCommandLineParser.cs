using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Config;
using DocumentFormat.OpenXml.Office2010.CustomUI;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class TrackerCommandLineParser : CommandLineParser
    {
        public TrackerCommandLineParser(IHelpGenerator generator) : base(generator)
        {
            Add(CommandLineOptionType.Help, false, "--help", "-h", "Show command line help", 0, 0);
            Add(CommandLineOptionType.ApplicationTimeout, false, "--app-timeout", "-a", "Timeout (ms) after which the application will quit of no messages are recieved", 1, 1);
            Add(CommandLineOptionType.AutoLookup, false, "--auto-lookup", "-al", "Automatically lookup aircraft and flights via the external APIs", 1, 1);
            Add(CommandLineOptionType.ClearDown, false, "--cleardown", "-cd", "Delete tracking records from the database before starting", 1, 1);
            Add(CommandLineOptionType.EnableSqlWriter, false, "--enable-sql-writer", "-w", "Log file path and name", 1, 1);
            Add(CommandLineOptionType.FlightApi, false, "--flight-api", "-fapi", "Specify the name of an API to use for lookups", 1, 1);
            Add(CommandLineOptionType.Host, false, "--host", "-ho", "Host to connect to for data stream", 1, 1);
            Add(CommandLineOptionType.LogFile, false, "--log-file", "-l", "Log file path and name", 1, 1);
            Add(CommandLineOptionType.MaximumRows, false, "--max-rows", "-m", "Maximum number of rows displayed", 1, 1);
            Add(CommandLineOptionType.MaximumTrackedAltitude, false, "--max-altitude", "-maxa", "Maximum altitude (ft) at which an aircraft will be tracked", 1, 1);
            Add(CommandLineOptionType.MaximumTrackedDistance, false, "--max-distance", "-maxd", "Maximum distance (Nm) at which an aircraft will be tracked", 1, 1);
            Add(CommandLineOptionType.MinimumLogLevel, false, "--log-level", "-ll", "Minimum logging level (Debug, Info, Warning or Error)", 1, 1);
            Add(CommandLineOptionType.MinimumTrackedAltitude, false, "--min-altitude", "-mina", "Minimum altitude (ft) at which an aircraft will be tracked", 1, 1);
            Add(CommandLineOptionType.Port, false, "--port", "-p", "Port to connect to for data stream", 1, 1);
            Add(CommandLineOptionType.Profile, false, "--tracking-profile", "-tpro", "Specify a JSON format tracking profile", 1, 1);
            Add(CommandLineOptionType.ReceiverLatitude, false, "--latitude", "-la", "Receiver latitude", 1, 1);
            Add(CommandLineOptionType.ReceiverLongitude, false, "--longitude", "-lo", "Receiver longitude", 1, 1);
            Add(CommandLineOptionType.RestartOnTimeout, false, "--auto-restart", "-ar", "Automatically restart the tracker after a timeout", 1, 1);
            Add(CommandLineOptionType.SettingsFile, false, "--settings", "-s", "Specify an alternative application settings file", 1, 1);
            Add(CommandLineOptionType.SocketReadTimeout, false, "--read-timeout", "-t", "Timeout (ms) for socket read operations", 1, 1);
            Add(CommandLineOptionType.TimeToLock, false, "--lock", "-k", "Time (ms) to locking of active database records", 1, 1);
            Add(CommandLineOptionType.TimeToRecent, false, "--recent", "-r", "Time (ms) to 'recent' staleness", 1, 1);
            Add(CommandLineOptionType.TimeToRemoval, false, "--remove", "-x", "Time (ms) to removal of stale records", 1, 1);
            Add(CommandLineOptionType.TimeToStale, false, "--stale", "-st", "Time (ms) to 'stale' staleness", 1, 1);
            Add(CommandLineOptionType.TrackedBehaviours, false, "--behaviours", "-b", "Specify a list of aircraft behaviours to track", 1, 1);
            Add(CommandLineOptionType.TrackPosition, false, "--track-position", "-tpos", "Set to true to record aircraft position over time", 1, 1);
            Add(CommandLineOptionType.VerboseLogging, false, "--verbose", "-v", "Enable verbose logging at debug log level", 1, 1);
        }
    }
}
