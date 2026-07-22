using BaseStationReader.Entities.Api;
using Spectre.Console;

namespace BaseStationReader.Lookup.Logic;

/// <summary>
/// Renders airport schedule mappings as a console table.
/// </summary>
internal sealed class ScheduleTabulator
{
    /// <summary>
    /// Writes all returned schedule rows to the console without persisting them.
    /// </summary>
    /// <param name="iata">The schedule airport IATA code.</param>
    /// <param name="from">The beginning of the lookup range.</param>
    /// <param name="to">The end of the lookup range.</param>
    /// <param name="mappings">The returned schedule rows.</param>
    public void Write(string iata, DateTime from, DateTime to, IEnumerable<FlightScheduleEntry> mappings)
    {
        var rows = mappings?.ToList() ?? [];
        AnsiConsole.MarkupLine($"[bold]Schedule for {Markup.Escape(iata)}[/] " +
            $"[grey]({from:g} to {to:g}, {rows.Count} flights)[/]");

        var table = new Table().Border(TableBorder.Rounded);
        foreach (var heading in new[]
        {
            "Callsign", "Flight IATA", "Airline IATA", "Airline ICAO", "Airline Name",
            "Airport IATA", "Airport ICAO", "Airport Name", "Embarkation", "Destination", "Direction"
        })
        {
            table.AddColumn(heading);
        }

        foreach (var mapping in rows)
        {
            table.AddRow(
                Cell(mapping.Callsign), Cell(mapping.FlightIATA), Cell(mapping.AirlineIATA),
                Cell(mapping.AirlineICAO), Cell(mapping.AirlineName), Cell(mapping.AirportIATA),
                Cell(mapping.AirportICAO), Cell(mapping.AirportName), Cell(mapping.Embarkation),
                Cell(mapping.Destination), Cell(mapping.AirportType.ToString()));
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Escapes a provider value for safe Spectre Console rendering.
    /// </summary>
    private static string Cell(string value)
        => Markup.Escape(string.IsNullOrWhiteSpace(value) ? "—" : value);
}
