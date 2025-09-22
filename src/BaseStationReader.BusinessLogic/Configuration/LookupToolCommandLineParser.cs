using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class LookupToolCommandLineParser : CommandLineParser
    {
        public LookupToolCommandLineParser(IHelpGenerator generator) : base(generator)
        {
            Add(CommandLineOptionType.Help, false, "--help", "-h", "Show command line help", 0, 0);
            Add(CommandLineOptionType.AircraftAddress, false, "--address", "-a", "Specify a 24-bit ICAO aircraft address to look up", 1, 1);
            Add(CommandLineOptionType.Departure, false, "--departure", "-d", "Specify a comma-separated list of departure airport ICAO/IATA codes", 1, 1);
            Add(CommandLineOptionType.Arrival, false, "--arrival", "-ar", "Specify a comma-separated list of arrival airport ICAO/IATA codes", 1, 1);
            Add(CommandLineOptionType.ImportAirlines, false, "--import-airlines", "-ia", "Import a set of airline definitions from a CSV file", 1, 1);
        }
    }
}
