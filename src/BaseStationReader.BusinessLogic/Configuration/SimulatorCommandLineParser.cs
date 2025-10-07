using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Config;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class SimulatorCommandLineParser : CommandLineParser
    {
        public SimulatorCommandLineParser(IHelpGenerator generator) : base(generator)
        {
            Add(CommandLineOptionType.Help, false, "--help", "-h", "Show command line help",0, 0);
            Add(CommandLineOptionType.Port, false, "--port", "-p", "Port to send data on", 1, 1);
            Add(CommandLineOptionType.SendInterval, false, "--send-interval", "-s", "Message send interval (ms)", 1, 1);
            Add(CommandLineOptionType.NumberOfAircraft, false, "--number", "-n", "Number of concurrent simulated aircraft", 1, 1);
            Add(CommandLineOptionType.MinimumAircraftLifespan, false, "--min-lifespan", "-minls", "Minimum simulated aircraft lifespan (ms)", 1, 1);
            Add(CommandLineOptionType.MaximumAircraftLifespan, false, "--max-lifespan", "-maxls", "Maximum simulated aircraft lifespan (ms)", 1, 1);
            Add(CommandLineOptionType.LogFile, false, "--log-file", "-l", "Log file path and name", 1, 1);
            Add(CommandLineOptionType.MinimumLogLevel, false, "--log-level", "-ll", "Minimum logging level (Debug, Info, Warning or Error)", 1, 1);
            Add(CommandLineOptionType.MinimumAltitude, false, "--min-altitude", "-minalt", "Minimum altitude in metres for aircraft in level flight", 1, 1);
            Add(CommandLineOptionType.MaximumAltitude, false, "--max-altitude", "-maxalt", "Maximum altitude in metres for aircraft in level flight", 1, 1);
            Add(CommandLineOptionType.MinimumTakeOffSpeed, false, "--min-takeoffspeed", "-mintos", "Minimum take off speed in m/s", 1, 1);
            Add(CommandLineOptionType.MaximumTakeOffSpeed, false, "--max-takeoffspeed", "-maxtos", "Maximum take off speed in m/s", 1, 1);
            Add(CommandLineOptionType.MinimumApproachSpeed, false, "--min-approachspeed", "-minas", "Minimum approach speed in m/s", 1, 1);
            Add(CommandLineOptionType.MaximumApproachSpeed, false, "--max-approachspeed", "-maxas", "Maximum approach speed in m/s", 1, 1);
            Add(CommandLineOptionType.MinimumCruisingSpeed, false, "--min-cruisespeed", "-mincs", "Minimum cruising speed in m/s", 1, 1);
            Add(CommandLineOptionType.MaximumCruisingSpeed, false, "--max-cruisespeed", "-maxcs", "Maximum cruising speed in m/s", 1, 1);
            Add(CommandLineOptionType.MinimumClimbRate, false, "--min-climbrate", "-mincr", "Minimum climbing rate in m/s", 1, 1);
            Add(CommandLineOptionType.MaximumClimbRate, false, "--max-climbrate", "-maxcr", "Maximum climbing rate in m/s", 1, 1);
            Add(CommandLineOptionType.MinimumDescentRate, false, "--min-descentrate", "-mindr", "Minimum descent rate in m/s", 1, 1);
            Add(CommandLineOptionType.MaximumDescentRate, false, "--max-descentrate", "-maxdr", "Maximum descent rate in m/s", 1, 1);
            Add(CommandLineOptionType.MaximumInitialRange, false, "--max-range", "--maxr", "Maximum initial distance to an aircraft in m", 1, 1);
            Add(CommandLineOptionType.ReceiverLatitude, false, "--latitude", "-la", "Receiver latitude", 1, 1);
            Add(CommandLineOptionType.ReceiverLongitude, false, "--longitude", "-lo", "Receiver latitude", 1, 1);
            Add(CommandLineOptionType.AddressFile, false, "--address-file", "-af", "Specify a text file containing a list of aircraft addresses to use", 1, 1);
        }
    }
}
