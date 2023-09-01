namespace BaseStationReader.Entities.Config
{
    public enum CommandLineOptionType
    {
        MinimumLogLevel,
        Host,
        Port,
        SocketReadTimeout,
        ApplicationTimeout,
        TimeToRecent,
        TimeToStale,
        TimeToRemoval,
        LogFile,
        EnableSqlWriter,
        WriterInterval,
        WriterBatchSize,
        PositionInterval,
        MaximumRows
    }
}
