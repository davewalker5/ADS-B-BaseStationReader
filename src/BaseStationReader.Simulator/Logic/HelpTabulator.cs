using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using Spectre.Console;

namespace BaseStationReader.Simulator.Logic
{
    public class HelpTabulator : IHelpGenerator
    {
        /// <summary>
        /// Tabulate a collection of available command line options
        /// </summary>
        /// <param name="options"></param>
        public void Generate(IEnumerable<CommandLineOption> options)
        {
            var table = new Table();

            table.AddColumn("Option");
            table.AddColumn("Short Form");
            table.AddColumn("Min Values");
            table.AddColumn("Max Values");
            table.AddColumn("Description");

            foreach (var option in options)
            {
                var rowData = new string[] {
                    GetCellData(option.Name),
                    GetCellData(option.ShortName),
                    GetCellData(option.MinimumNumberOfValues.ToString()),
                    GetCellData(option.MaximumNumberOfValues.ToString()),
                    GetCellData(option.Description)
                };

                table.AddRow(rowData);
            }

            AnsiConsole.Write(table);
        }

        private string GetCellData(string value)
            => $"[white]{value}[/]";
    }
}
