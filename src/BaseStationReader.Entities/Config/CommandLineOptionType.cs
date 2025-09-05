namespace BaseStationReader.Entities.Config
{
    public enum CommandLineOptionType
    {
        Help,
        MinimumLogLevel,
        Host,
        Port,
        SocketReadTimeout,
        ApplicationTimeout,
        RestartOnTimeout,
        TimeToRecent,
        TimeToStale,
        TimeToRemoval,
        TimeToLock,
        LogFile,
        EnableSqlWriter,
        WriterInterval,
        WriterBatchSize,
        RefreshInterval,
        MaximumRows,
        ReceiverLatitude,
        ReceiverLongitude,
        SendInterval,
        NumberOfAircraft,
        Lifespan
    }
}
