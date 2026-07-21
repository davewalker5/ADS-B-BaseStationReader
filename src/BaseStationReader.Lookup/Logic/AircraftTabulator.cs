using BaseStationReader.Entities.Api;
using Spectre.Console;

namespace BaseStationReader.Lookup.Logic;

/// <summary>
/// Renders aircraft lookup results as a property/value table.
/// </summary>
internal sealed class AircraftTabulator
{
    public void Write(string address, Aircraft aircraft)
    {
        var table = new Table().Border(TableBorder.Rounded);
        table.AddColumn("Property");
        table.AddColumn("Value");

        Add(table, "Aircraft Address", address);
        if (aircraft == null)
        {
            Add(table, "Result", "Aircraft not found");
        }
        else
        {
            Add(table, "Registration", aircraft.Registration);
            Add(table, "Callsign", aircraft.Callsign);
            Add(table, "Manufactured", aircraft.Manufactured?.ToString());
            Add(table, "Age", aircraft.Age?.ToString());
            Add(table, "Model", aircraft.Model?.Name);
            Add(table, "Model IATA", aircraft.Model?.IATA ?? aircraft.ModelIATA);
            Add(table, "Model ICAO", aircraft.Model?.ICAO ?? aircraft.ModelICAO);
            Add(table, "Manufacturer", aircraft.Model?.Manufacturer?.Name ?? aircraft.Model?.ManufacturerName);
        }

        AnsiConsole.Write(table);
    }

    private static void Add(Table table, string property, string value)
        => table.AddRow(Markup.Escape(property), Markup.Escape(string.IsNullOrWhiteSpace(value) ? "—" : value));
}
