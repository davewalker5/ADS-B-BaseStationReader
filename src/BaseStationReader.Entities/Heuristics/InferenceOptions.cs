using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Heuristics
{
    [ExcludeFromCodeCoverage]
    public class InferenceOptions
    {
        public int NumericSuffixMinimumSupport { get; set; } = 3;
        public decimal NumericSuffixMinimumPurity { get; set; } = 0.9M;
        public int SuffixDeltaMinimumSupport { get; set; } = 3;
        public decimal SuffixDeltaMinimumPurity { get; set; } = 0.85M;
        public decimal MinimumIdentityPurity { get; set; } = 0.9M;
    }
}
