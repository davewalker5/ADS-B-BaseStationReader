using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Logic.Configuration
{
    public class SimulatorSettingsBuilder
    {
        /// <summary>
        /// Construct the application settings from the configuration file and any command line arguments
        /// </summary>
        /// <param name="args"></param>
        /// <param name="configJsonPath"></param>
        /// <returns></returns>
#pragma warning disable S3776
        public SimulatorApplicationSettings? BuildSettings(IEnumerable<string> args, string configJsonPath)
        {
            // Read the config file to provide default settings
            var settings = new ConfigReader<SimulatorApplicationSettings>().Read(configJsonPath);

            // Parse the command line
            var parser = new CommandLineParser();
            parser.Add(CommandLineOptionType.Port, false, "--port", "-p", "Port to send data on", 1, 1);
            parser.Add(CommandLineOptionType.SendInterval, false, "--send-interval", "-s", "Message send interval (ms)", 1, 1);
            parser.Add(CommandLineOptionType.NumberOfAircraft, false, "--number", "-n", "Number of concurrent simulated aircraft", 1, 1);
            parser.Add(CommandLineOptionType.Lifespan, false, "--lifespan", "-ls", "Simulated aircraft lifespan (ms)", 1, 1);
            parser.Add(CommandLineOptionType.LogFile, false, "--log-file", "-l", "Log file path and name", 1, 1);
            parser.Add(CommandLineOptionType.MinimumLogLevel, false, "--log-level", "-ll", "Minimum logging level (Debug, Info, Warning or Error)", 1, 1);
            parser.Parse(args);

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