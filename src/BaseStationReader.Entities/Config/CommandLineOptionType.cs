namespace BaseStationReader.Entities.Config
{
    public enum CommandLineOptionType
    {
        Host,
        Port,
        SocketReadTimeout,
        TimeToRecent,
        TimeToStale,
        TimeToRemoval,
        LogFile,
        EnableSqlWriter,
        WriterInterval,
        WriterBatchSize
    }
}
