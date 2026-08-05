using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Config;
using Spectre.Console;

namespace BaseStationReader.SpoolReplayer.Logic;

/// <summary>
/// Displays command-line help as a console table.
/// </summary>
public sealed class HelpTabulator : IHelpGenerator
{
    /// <inheritdoc />
    public void Generate(IEnumerable<CommandLineOption> options)
    {
        var table = new Table();
        table.AddColumn("Option");
        table.AddColumn("Short Form");
        table.AddColumn("Min Values");
        table.AddColumn("Max Values");
        table.AddColumn("Description");

        foreach (var option in options.OrderBy(
            option => option.Name.TrimStart('-'),
            StringComparer.OrdinalIgnoreCase))
        {
            table.AddRow(
                GetCellData(option.Name),
                GetCellData(option.ShortName),
                GetCellData(option.MinimumNumberOfValues.ToString()),
                GetCellData(option.MaximumNumberOfValues.ToString()),
                GetCellData(option.Description));
        }

        AnsiConsole.Write(table);
    }

    /// <summary>
    /// Escapes and formats a help-table cell.
    /// </summary>
    /// <param name="value">Cell value.</param>
    /// <returns>Spectre.Console markup.</returns>
    private static string GetCellData(string value)
        => $"[white]{Markup.Escape(value)}[/]";
}
