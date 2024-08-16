using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Logic.Configuration
{
    public class SimulatorSettingsBuilder : ISimulatorSettingsBuilder
    {
        /// <summary>
        /// Construct the application settings from the configuration file and any command line arguments
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="configJsonPath"></param>
        /// <returns></returns>
#pragma warning disable S3776
        public SimulatorApplicationSettings? BuildSettings(ICommandLineParser parser, string configJsonPath)
        {
            // Read the config file to provide default settings
            var settings = new ConfigReader<SimulatorApplicationSettings>().Read(configJsonPath);

            // Apply the command line values over the defaults
            var values = parser.GetValues(CommandLineOptionType.Port);
            if (values != null) settings!.Port = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.SendInterval);
            if (values != null) settings!.SendInterval = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.NumberOfAircraft);
            if (values != null) settings!.NumberOfAircraft = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.Lifespan);
            if (values != null) settings!.AircraftLifespan = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.LogFile);
            if (values != null) settings!.LogFile = values[0];

            values = parser.GetValues(CommandLineOptionType.MinimumLogLevel);
            if (values != null && Enum.TryParse<Severity>(values[0], out Severity minimumLogLevel))
            {
                settings!.MinimumLogLevel = minimumLogLevel;
            }

            return settings;
        }
# pragma warning restore S3776
    }
}