using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class LookupToolSettingsBuilder : ConfigReader<LookupToolApplicationSettings>, ILookupToolSettingsBuilder
    {
        /// <summary>
        /// Construct the application settings from the configuration file and any command line arguments
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="configJsonPath"></param>
        /// <returns></returns>
        public LookupToolApplicationSettings BuildSettings(ICommandLineParser parser, string configJsonPath)
        {
            // Read the config file to provide default settings
            var settings = base.Read(configJsonPath);

            var values = parser.GetValues(CommandLineOptionType.LogFile);
            if (values != null) settings.LogFile = values[0];

            values = parser.GetValues(CommandLineOptionType.MinimumLogLevel);
            if (values != null && Enum.TryParse<Severity>(values[0], out Severity minimumLogLevel))
            {
                settings.MinimumLogLevel = minimumLogLevel;
            }

            values = parser.GetValues(CommandLineOptionType.CreateSightings);
            if (values != null) settings.CreateSightings = bool.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.LiveApi);
            if (values != null) settings.LiveApi = values[0];

            values = parser.GetValues(CommandLineOptionType.ReceiverLatitude);
            if (values != null) settings.ReceiverLatitude = double.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ReceiverLongitude);
            if (values != null) settings.ReceiverLongitude = double.Parse(values[0]);

            return settings;
        }
    }
}