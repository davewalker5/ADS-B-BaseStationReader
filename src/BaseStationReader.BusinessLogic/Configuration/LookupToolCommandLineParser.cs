using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Config;

namespace BaseStationReader.BusinessLogic.Configuration
{
    public class LookupToolCommandLineParser : CommandLineParser
    {
        public LookupToolCommandLineParser(IHelpGenerator generator) : base(generator)
        {
            Add(CommandLineOptionType.Help, false, "--help", "-h", "Show command line help", 0, 0);
            Add(CommandLineOptionType.AddExcludedAddress, false, "--exclude-address", "-exa", "Add an aircraft address to the excluded addresses list", 1, 1);
            Add(CommandLineOptionType.AddExcludedCallsign, false, "--exclude-callsign", "-exc", "Add a callsign to the excluded callsigns list", 1, 1);
            Add(CommandLineOptionType.Aircraft, false, "--aircraft", "-a", "Look up an aircraft by ICAO address", 1, 1);
            Add(CommandLineOptionType.AircraftApi, false, "--aircraft-api", "-aapi", "Specify the name of an API to use for aircraft lookups", 1, 1);
            Add(CommandLineOptionType.ResolveAircraft, false, "--resolve-tracked", "-rt", "Resolve tracked aircraft and flight details using local reference data", 0, 0);
            Add(CommandLineOptionType.Arrival, false, "--arrival", "-ar", "Specify a comma-separated list of arrival airport ICAO/IATA codes", 1, 1);
            Add(CommandLineOptionType.CreateSightings, false, "--create-sightings", "-cs", "If true, create sightings relating flights and aircraft when a lookup is completed", 1, 1);
            Add(CommandLineOptionType.Departure, false, "--departure", "-d", "Specify a comma-separated list of departure airport ICAO/IATA codes", 1, 1);
            Add(CommandLineOptionType.AirportSchedule, false, "--airport-schedule", "-as", "Look up and tabulate schedule information for an airport", 1, 3);
            Add(CommandLineOptionType.Flight, false, "--flight", "-f", "Look up a flight by callsign", 1, 1);
            Add(CommandLineOptionType.FlightApi, false, "--flight-api", "-fapi", "Specify the name of an API to use for flight lookups", 1, 1);
            Add(CommandLineOptionType.ImportAircraft, false, "--import-aircraft", "-imac", "Import a set of aircraft definitions from a CSV file", 1, 1);
            Add(CommandLineOptionType.ImportAirlines, false, "--import-airlines", "-ia", "Import a set of airline definitions from a CSV file", 1, 1);
            Add(CommandLineOptionType.ImportAirports, false, "--import-airports", "-iap", "Import a set of airport definitions from a CSV file", 1, 1);
            Add(CommandLineOptionType.ImportFlightIATACodeMappings, false, "--import-mappings", "-im", "Import a set of callsign/flight IATA code mappings from a CSV file", 1, 1);
            Add(CommandLineOptionType.ImportManufacturers, false, "--import-manufacturers", "-ima", "Import a set of manufacturer definitions from a CSV file", 1, 1);
            Add(CommandLineOptionType.ImportModels, false, "--import-models", "-imo", "Import a set of model definitions from a CSV file", 1, 1);
            Add(CommandLineOptionType.ListExclusions, false, "--list-exclusions", "-lex", "List the excluded aircraft addresses and callsigns", 0, 0);
            Add(CommandLineOptionType.LogFile, false, "--log-file", "-l", "Log file path and name", 1, 1);
            Add(CommandLineOptionType.METAR, false, "--metar", "-m", "Lookup the current weather for a given airport ICAO code", 1, 1);
            Add(CommandLineOptionType.MinimumLogLevel, false, "--log-level", "-ll", "Minimum logging level (Debug, Info, Warning or Error)", 1, 1);
            Add(CommandLineOptionType.ReceiverLatitude, false, "--latitude", "-la", "Receiver latitude", 1, 1);
            Add(CommandLineOptionType.ReceiverLongitude, false, "--longitude", "-lo", "Receiver latitude", 1, 1);
            Add(CommandLineOptionType.TAF, false, "--taf", "-t", "Lookup the weather forecast for a given airport ICAO code", 1, 1);
            Add(CommandLineOptionType.WeatherApi, false, "--weather-api", "-wapi", "Specify the name of an API to use for weather lookups", 1, 1);       }
    }
}
