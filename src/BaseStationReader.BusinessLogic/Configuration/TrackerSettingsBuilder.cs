using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Config;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class TrackerSettingsBuilder : ITrackerSettingsBuilder
    {
        /// <summary>
        /// Construct the application settings from the configuration file and any command line arguments
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="reader"></param>
        /// <param name="defaultConfigJsonPath"></param>
        /// <returns></returns>
        public TrackerApplicationSettings BuildSettings(
            ICommandLineParser parser,
            ITrackingProfileReaderWriter reader,
            string defaultConfigJsonPath)
        {
            // If a settings file has been specified, use it in place of the default
            var values = parser.GetValues(CommandLineOptionType.SettingsFile);
            var configJsonPath = (values != null) ? values[0] : defaultConfigJsonPath;

            // Read the config file to provide default settings
            var settings = new TrackerConfigReader().Read(configJsonPath);
            settings.TrackingProfile = Path.GetFileName(configJsonPath);

            // Apply the command line values over the defaults
            values = parser.GetValues(CommandLineOptionType.Host);
            if (values != null) settings.Host = values[0];

            values = parser.GetValues(CommandLineOptionType.Port);
            if (values != null) settings.Port = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.SocketReadTimeout);
            if (values != null) settings.SocketReadTimeout = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ApplicationTimeout);
            if (values != null) settings.ApplicationTimeout = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.RestartOnTimeout);
            if (values != null) settings.RestartOnTimeout = bool.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToRecent);
            if (values != null) settings.TimeToRecent = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToStale);
            if (values != null) settings.TimeToStale = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToRemoval);
            if (values != null) settings.TimeToRemoval = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TimeToLock);
            if (values != null) settings.TimeToLock = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.LogFile);
            if (values != null) settings.LogFile = values[0];

            values = parser.GetValues(CommandLineOptionType.MinimumLogLevel);
            if (values != null && Enum.TryParse<Severity>(values[0], out Severity minimumLogLevel))
            {
                settings.MinimumLogLevel = minimumLogLevel;
            }

            values = parser.GetValues(CommandLineOptionType.VerboseLogging);
            if (values != null) settings.VerboseLogging = bool.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.EnableSqlWriter);
            if (values != null) settings.EnableSqlWriter = bool.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ClearDown);
            if (values != null) settings.ClearDown = bool.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.AutoLookup);
            if (values != null) settings.AutoLookup = bool.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumRows);
            if (values != null) settings.MaximumRows = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ReceiverLatitude);
            if (values != null) settings.ReceiverLatitude = double.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ReceiverLongitude);
            if (values != null) settings.ReceiverLongitude = double.Parse(values[0]);
            
            values = parser.GetValues(CommandLineOptionType.MaximumTrackedDistance);
            if (values != null) settings.MaximumTrackedDistance = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MinimumTrackedAltitude);
            if (values != null) settings.MinimumTrackedAltitude = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumTrackedAltitude);
            if (values != null) settings.MaximumTrackedAltitude = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TrackPosition);
            if (values != null) settings.TrackPosition = bool.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.TrackedBehaviours);
            if (values != null)
            {
                settings.TrackedBehaviours = values[0]
                    .Split(',', StringSplitOptions.RemoveEmptyEntries)
                    .Select(s => Enum.Parse<AircraftBehaviour>(s))
                    .ToList();
            }

            values = parser.GetValues(CommandLineOptionType.FlightApi);
            if (values != null) settings.FlightApi = values[0];

            // If a profile's been specified, read it and override the tracking parameters specified
            // in the settings file and/or via the command line
            values = parser.GetValues(CommandLineOptionType.Profile);
            if (values != null)
            {
                var profile = reader.Read(values[0]);
                settings.TrackingProfile = Path.GetFileName(values[0]);
                settings.ReceiverLatitude = profile.ReceiverLatitude;
                settings.ReceiverLongitude = profile.ReceiverLongitude;
                settings.MinimumTrackedAltitude = profile.MinimumTrackedAltitude;
                settings.MaximumTrackedAltitude = profile.MaximumTrackedAltitude;
                settings.MaximumTrackedDistance = profile.MaximumTrackedDistance;
                settings.TrackedBehaviours = profile.TrackedBehaviours;
            }

            return settings;
        }
    }
}
