using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.Logic.Configuration
{
    public class TrackerCommandLineParser : CommandLineParser
    {
        public TrackerCommandLineParser(IHelpGenerator generator) : base(generator)
        {
            Add(CommandLineOptionType.Help, false, "--help", "-h", "Show command line help", 0, 0);
            Add(CommandLineOptionType.Host, false, "--host", "-ho", "Host to connect to for data stream", 1, 1);
            Add(CommandLineOptionType.Port, false, "--port", "-p", "Port to connect to for data stream", 1, 1);
            Add(CommandLineOptionType.SocketReadTimeout, false, "--read-timeout", "-t", "Timeout (ms) for socket read operations", 1, 1);
            Add(CommandLineOptionType.ApplicationTimeout, false, "--app-timeout", "-a", "Timeout (ms) after which the application will quit of no messages are recieved", 1, 1);
            Add(CommandLineOptionType.RestartOnTimeout, false, "--auto-restart", "-ar", "Automatically restart the tracker after a timeout", 1, 1);
            Add(CommandLineOptionType.TimeToRecent, false, "--recent", "-r", "Time (ms) to 'recent' staleness", 1, 1);
            Add(CommandLineOptionType.TimeToStale, false, "--stale", "-s", "Time (ms) to 'stale' staleness", 1, 1);
            Add(CommandLineOptionType.TimeToRemoval, false, "--remove", "-x", "Time (ms) to removal of stale records", 1, 1);
            Add(CommandLineOptionType.TimeToLock, false, "--lock", "-k", "Time (ms) to locking of active database records", 1, 1);
            Add(CommandLineOptionType.LogFile, false, "--log-file", "-l", "Log file path and name", 1, 1);
            Add(CommandLineOptionType.MinimumLogLevel, false, "--log-level", "-ll", "Minimum logging level (Debug, Info, Warning or Error)", 1, 1);
            Add(CommandLineOptionType.EnableSqlWriter, false, "--enable-sql-writer", "-w", "Log file path and name", 1, 1);
            Add(CommandLineOptionType.WriterInterval, false, "--writer-interval", "-i", "SQL write interval (ms)", 1, 1);
            Add(CommandLineOptionType.WriterBatchSize, false, "--writer-batch-size", "-b", "SQL write batch size", 1, 1);
            Add(CommandLineOptionType.RefreshInterval, false, "--ui-interval", "-ui", "Interval (ms) at which live UI's should update", 1, 1);
            Add(CommandLineOptionType.MaximumRows, false, "--max-rows", "-m", "Maximum number of rows displayed", 1, 1);
            Add(CommandLineOptionType.ReceiverLatitude, false, "--latitude", "-la", "Receiver latitude", 1, 1);
            Add(CommandLineOptionType.ReceiverLongitude, false, "--longitude", "-lo", "Receiver latitude", 1, 1);
        }
    }
}
