using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.Logic.Configuration
{
    public class SimulatorCommandLineParser : CommandLineParser
    {
        public SimulatorCommandLineParser(IHelpGenerator generator) : base(generator)
        {
            Add(CommandLineOptionType.Help, false, "--help", "-h", "Show command line help",0, 0);
            Add(CommandLineOptionType.Port, false, "--port", "-p", "Port to send data on", 1, 1);
            Add(CommandLineOptionType.SendInterval, false, "--send-interval", "-s", "Message send interval (ms)", 1, 1);
            Add(CommandLineOptionType.NumberOfAircraft, false, "--number", "-n", "Number of concurrent simulated aircraft", 1, 1);
            Add(CommandLineOptionType.Lifespan, false, "--lifespan", "-ls", "Simulated aircraft lifespan (ms)", 1, 1);
            Add(CommandLineOptionType.LogFile, false, "--log-file", "-l", "Log file path and name", 1, 1);
            Add(CommandLineOptionType.MinimumLogLevel, false, "--log-level", "-ll", "Minimum logging level (Debug, Info, Warning or Error)", 1, 1);
        }
    }
}
