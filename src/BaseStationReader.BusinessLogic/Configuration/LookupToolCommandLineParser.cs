using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Config;

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
            Add(CommandLineOptionType.ImportManufacturers, false, "--import-manufacturers", "-ima", "Import a set of manufacturer definitions from a CSV file", 1, 1);
            Add(CommandLineOptionType.ImportModels, false, "--import-models", "-imo", "Import a set of model definitions from a CSV file", 1, 1);
            Add(CommandLineOptionType.CreateSightings, false, "--create-sightings", "-cs", "If true, create sightings relating flights and aircraft when a lookup is completed", 1, 1);
            Add(CommandLineOptionType.LiveApi, false, "--live-api", "-lapi", "Specify the name of an API to use for live flight lookups", 1, 1);
            Add(CommandLineOptionType.HistoricalApi, false, "--historical-api", "-hapi", "Specify the name of an API to use for historical flight lookups", 1, 1);
            Add(CommandLineOptionType.ReceiverLatitude, false, "--latitude", "-la", "Receiver latitude", 1, 1);
            Add(CommandLineOptionType.ReceiverLongitude, false, "--longitude", "-lo", "Receiver latitude", 1, 1);
            Add(CommandLineOptionType.FlightsInRange, false, "--export-in-range", "-eir", "Export flights within the specified range (Nm) of the receiver to a CSV file", 2, 2);
            Add(CommandLineOptionType.HistoricalLookup, false, "--historical-lookup", "-hl", "Lookup all tracked aircraft that have not already been looked up", 0, 0);
            Add(CommandLineOptionType.METAR, false, "--metar", "-m", "Lookup the current weather for a given airport ICAO code", 1, 1);
            Add(CommandLineOptionType.TAF, false, "--taf", "-t", "Lookup the weather forecast for a given airport ICAO code", 1, 1);
            Add(CommandLineOptionType.WeatherApi, false, "--weather-api", "-wapi", "Specify the name of an API to use for weather lookups", 1, 1);
            Add(CommandLineOptionType.ConvertCallsigns, false, "--convert-callsigns", "-cc", "Convert tracked aircraft callsigns to flight numbers and export to a CSV file", 1, 1);
            Add(CommandLineOptionType.ImportConfirmedMappings, false, "--import-mappings", "-im", "Import confirmed callsign/flight number mappings from a CSV file", 1, 1);
            Add(CommandLineOptionType.ImportNumberSuffixRules, false, "--import-number-suffix", "-ins", "Import number/suffix rules from a CSV file", 1, 1);
        }
    }
}
