using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Heuristics
{
    [ExcludeFromCodeCoverage]
    public class SuffixDeltaRule : HeuristicModelBase
    {
        public string Suffix { get; set; }
        public int Delta { get; set; }
        public int Support { get; set; }
        public decimal Purity { get; set; }
    }
}