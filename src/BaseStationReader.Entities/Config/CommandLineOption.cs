using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class CommandLineOption
    {
        public CommandLineOptionType OptionType { get; set; }
        public bool Mandatory { get; set; } = false;
        public string Name { get; set; } = "";
        public string ShortName { get; set; } = "";
        public string Description { get; set; } = "";
        public int MinimumNumberOfValues { get; set; } = 1;
        public int MaximumNumberOfValues { get; set; } = 1;
    }
}
