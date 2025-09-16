using BaseStationReader.Entities.Logging;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class SimulatorApplicationSettings
    {
        public Severity MinimumLogLevel { get; set; }
        public string LogFile { get; set; } = "";
        public int Port { get; set; }
        public int SendInterval { get; set; }
        public int NumberOfAircraft { get; set; }
        public int AircraftLifespan { get; set; }
        public int MinimumAltitude { get; set; }
        public int MaximumAltitude { get; set; }
        public int MinimumTakeOffSpeed { get; set; }
        public int MaximumTakeOffSpeed { get; set; }
        public int MinimumApproachSpeed { get; set; }
        public int MaximumApproachSpeed { get; set; }
        public int MinimumCruisingSpeed { get; set; }
        public int MaximumCruisingSpeed { get; set; }
        public int MinimumClimbRate { get; set; }
        public int MaximumClimbRate { get; set; }
        public decimal MinimumDescentRate { get; set; }
        public decimal MaximumDescentRate { get; set; }
        public double ReceiverLatitude { get; set; }
        public double ReceiverLongitude { get; set; }
        public int MaximumInitialRange { get; set; }
    }
}
