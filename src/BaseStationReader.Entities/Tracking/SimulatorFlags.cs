namespace BaseStationReader.Entities.Tracking
{
    [Flags]
    public enum SimulatorFlags
    {
        LevelFlight = 0x0,
        TakingOff = 0x01,
        Landing = 0x02
    }
}