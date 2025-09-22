using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class LookupToolSettingsBuilder : SettingsBuilderBase<LookupToolApplicationSettings>, ILookupToolSettingsBuilder
    {
        /// <summary>
        /// Construct the application settings from the configuration file and any command line arguments
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="configPath"></param>
        /// <returns></returns>
        public LookupToolApplicationSettings BuildSettings(ICommandLineParser parser, string configPath)
        {
            // Read the config file to provide default settings
            var settings = base.LoadSettings(parser, configPath);

            var values = parser.GetValues(CommandLineOptionType.LogFile);
            if (values != null) settings!.LogFile = values[0];

            values = parser.GetValues(CommandLineOptionType.MinimumLogLevel);
            if (values != null && Enum.TryParse<Severity>(values[0], out Severity minimumLogLevel))
            {
                settings.MinimumLogLevel = minimumLogLevel;
            }

            return settings;
        }
    }
}