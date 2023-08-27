namespace BaseStationReader.Entities.Config
{
    public enum CommandLineOptionType
    {
        Host,
        Port,
        TimeToRecent,
        TimeToStale,
        TimeToRemoval,
        LogFile,
        EnableSqlWriter,
        WriterInterval,
        WriterBatchSize
    }
}
