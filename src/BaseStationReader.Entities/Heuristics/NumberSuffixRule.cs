using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Heuristics
{
    [ExcludeFromCodeCoverage]
    public class NumberSuffixRule : HeuristicModelBase
    {
        public string Numeric { get; set; }
        public string Suffix { get; set; }
        public string Digits { get; set; }
        public int Support { get; set; }
        public decimal Purity { get; set; }
    }
}