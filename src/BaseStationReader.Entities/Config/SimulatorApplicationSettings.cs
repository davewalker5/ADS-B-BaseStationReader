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
    }
}
