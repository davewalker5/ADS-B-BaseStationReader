using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Tests
{
    public abstract class SimulatorTestBase
    {
        protected readonly SimulatorApplicationSettings _settings= new()
        {
            Port = 30003,
            SendInterval = 100,
            NumberOfAircraft = 10,
            AircraftLifespan = 60000,
            LogFile = "ReceiverSimulator.log",
            MinimumLogLevel = Severity.Info,
            MinimumAltitude = 4572,
            MaximumAltitude = 12192,
            MinimumTakeOffSpeed = 72,
            MaximumTakeOffSpeed = 82,
            MinimumApproachSpeed = 67,
            MaximumApproachSpeed = 76,
            MinimumCruisingSpeed = 231,
            MaximumCruisingSpeed = 247,
            MinimumClimbRate = 7,
            MaximumClimbRate = 20,
            MinimumDescentRate = 3.5M,
            MaximumDescentRate = 4M,
            MaximumInitialRange = 55560,
            ReceiverLatitude = 51.4711123,
            ReceiverLongitude = -0.4646874
        };
    }
}