using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Messages
{
    [ExcludeFromCodeCoverage]
    public class NumberSuffix : HeuristicModelBase
    {
        public string Numeric { get; set; }
        public string Suffix { get; set; }
        public string Digits { get; set; }
        public int Support { get; set; }
        public decimal Purity { get; set; }
    }
}