using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Heuristics
{
    [ExcludeFromCodeCoverage]
    public class ConfirmedMapping : HeuristicModelBase
    {
        public string FlightIATA { get; set; }
        public string Callsign { get; set; }
        public string Digits { get; set; }
    }
}