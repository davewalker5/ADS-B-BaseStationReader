using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Logic.Configuration
{
    public class TrackerSettingsBuilder : ITrackerSettingsBuilder
    {
        /// <summary>
        /// Construct the application settings from the configuration file and any command line arguments
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="configJsonPath"></param>
        /// <returns></returns>
#pragma warning disable S3776
        public TrackerApplicationSettings? BuildSettings(ICommandLineParser parser, string configJsonPath)
        {
            // Read the config file to provide default settings
            var settings = new TrackerConfigReader().Read(configJsonPath);

            // Apply the command line values over the defaults
            var values = parser.GetValues(CommandLineOptionType.Host);
            if (values != null) settings!.Host = values[0];

            values = parser.GetValues(CommandLineOptionType.Port);
            if (values != null) settings!.Port = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.SocketReadTimeout);
            if (values != null) settings!.SocketReadTimeout = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ApplicationTimeout);
            if (values != null) settings!.ApplicationTimeout = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.RestartOnTimeout);
            if (values != null) settings!.RestartOnTimeout = bool.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToRecent);
            if (values != null) settings!.TimeToRecent = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToStale);
            if (values != null) settings!.TimeToStale = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToRemoval);
            if (values != null) settings!.TimeToRemoval = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToLock);
            if (values != null) settings!.TimeToLock = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.LogFile);
            if (values != null) settings!.LogFile = values[0];

            values = parser.GetValues(CommandLineOptionType.MinimumLogLevel);
            if (values != null && Enum.TryParse<Severity>(values[0], out Severity minimumLogLevel))
            {
                settings!.MinimumLogLevel = minimumLogLevel;
            }

            values = parser.GetValues(CommandLineOptionType.EnableSqlWriter);
            if (values != null) settings!.EnableSqlWriter = bool.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.WriterInterval);
            if (values != null) settings!.WriterInterval = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.WriterBatchSize);
            if (values != null) settings!.WriterBatchSize = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.RefreshInterval);
            if (values != null) settings!.RefreshInterval = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumRows);
            if (values != null) settings!.MaximumRows = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ReceiverLatitude);
            if (values != null) settings!.ReceiverLatitude = double.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ReceiverLongitude);
            if (values != null) settings!.ReceiverLongitude = double.Parse(values[0]);

            return settings;
        }
# pragma warning restore S3776
    }
}
