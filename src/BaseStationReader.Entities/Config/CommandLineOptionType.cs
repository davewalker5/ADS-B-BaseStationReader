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
        TimeToLock,
        LogFile,
        EnableSqlWriter,
        WriterInterval,
        WriterBatchSize,
        RefreshInterval,
        MaximumRows
    }
}
