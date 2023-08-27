namespace BaseStationReader.Entities.Config
{
    public enum CommandLineOptionType
    {
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
        WriterBatchSize
    }
}
