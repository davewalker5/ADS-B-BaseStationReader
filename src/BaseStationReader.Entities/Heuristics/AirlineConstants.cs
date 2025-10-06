using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Heuristics
{
    [ExcludeFromCodeCoverage]
    public class AirlineConstants : HeuristicModelBase
    {
        public int? ConstantDelta { get; set; }
        public decimal ConstantDeltaPurity { get; set; }
        public string ConstantPrefix { get; set; }
        public decimal IdentityRate { get; set; }
    }
}