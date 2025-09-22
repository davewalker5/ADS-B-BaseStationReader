using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class SimulatorSettingsBuilder : SettingsBuilderBase<SimulatorApplicationSettings>, ISimulatorSettingsBuilder
    {
        /// <summary>
        /// Construct the application settings from the configuration file and any command line arguments
        /// </summary>
        /// <param name="parser"></param>
        /// <param name="configPath"></param>
        /// <returns></returns>
        public SimulatorApplicationSettings BuildSettings(ICommandLineParser parser, string configPath)
        {
            // Read the config file to provide default settings
            var settings = base.LoadSettings(parser, configPath);

            // Apply the command line values over the defaults
            var values = parser.GetValues(CommandLineOptionType.Port);
            if (values != null) settings.Port = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.SendInterval);
            if (values != null) settings.SendInterval = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.NumberOfAircraft);
            if (values != null) settings.NumberOfAircraft = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MinimumAircraftLifespan);
            if (values != null) settings.MinimumAircraftLifespan = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumAircraftLifespan);
            if (values != null) settings.MaximumAircraftLifespan = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.LogFile);
            if (values != null) settings.LogFile = values[0];

            values = parser.GetValues(CommandLineOptionType.MinimumLogLevel);
            if (values != null && Enum.TryParse<Severity>(values[0], out Severity minimumLogLevel))
            {
                settings.MinimumLogLevel = minimumLogLevel;
            }

            values = parser.GetValues(CommandLineOptionType.MinimumAltitude);
            if (values != null) settings.MinimumAltitude = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumAltitude);
            if (values != null) settings.MaximumAltitude = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MinimumTakeOffSpeed);
            if (values != null) settings.MinimumTakeOffSpeed = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumTakeOffSpeed);
            if (values != null) settings.MaximumTakeOffSpeed = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MinimumApproachSpeed);
            if (values != null) settings.MinimumApproachSpeed = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumApproachSpeed);
            if (values != null) settings.MaximumApproachSpeed = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MinimumCruisingSpeed);
            if (values != null) settings.MinimumCruisingSpeed = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumCruisingSpeed);
            if (values != null) settings.MaximumCruisingSpeed = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MinimumClimbRate);
            if (values != null) settings.MinimumClimbRate = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumClimbRate);
            if (values != null) settings.MaximumClimbRate = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MinimumDescentRate);
            if (values != null) settings.MinimumDescentRate = decimal.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumDescentRate);
            if (values != null) settings.MaximumDescentRate = decimal.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.MaximumInitialRange);
            if (values != null) settings.MaximumInitialRange = int.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ReceiverLatitude);
            if (values != null) settings.ReceiverLatitude = double.Parse(values[0]);

            values = parser.GetValues(CommandLineOptionType.ReceiverLongitude);
            if (values != null) settings.ReceiverLongitude = double.Parse(values[0]);

            return settings;
        }
    }
}