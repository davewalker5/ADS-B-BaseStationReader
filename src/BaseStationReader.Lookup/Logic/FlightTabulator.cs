using BaseStationReader.Entities.Api;
using Spectre.Console;

namespace BaseStationReader.Lookup.Logic;

/// <summary>
/// Renders flight lookup results as a property/value table.
/// </summary>
internal sealed class FlightTabulator
{
    public void Write(string callsign, Flight flight)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Property");
        table.AddColumn("Value");

        Add(table, "Callsign", callsign);
        if (flight == null)
        {
            Add(table, "Result", "Flight not found");
        }
        else
        {
            Add(table, "Flight IATA", flight.IATA);
            Add(table, "Flight ICAO", flight.ICAO);
            Add(table, "Embarkation", flight.Embarkation);
            Add(table, "Destination", flight.Destination);
            Add(table, "Airline", flight.Airline?.Name);
            Add(table, "Airline IATA", flight.Airline?.IATA);
            Add(table, "Airline ICAO", flight.Airline?.ICAO);
            Add(table, "Aircraft Address", flight.AircraftAddress);
            Add(table, "Aircraft Model ICAO", flight.ModelICAO);
        }

        AnsiConsole.Write(table);
    }

    private static void Add(Table table, string property, string value)
        => table.AddRow(Markup.Escape(property), Markup.Escape(string.IsNullOrWhiteSpace(value) ? "—" : value));
}
