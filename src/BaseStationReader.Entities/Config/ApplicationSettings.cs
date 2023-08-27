using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class ApplicationSettings
    {
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public int TimeToRecent { get; set; }
        public int TimeToStale { get; set; }
        public int TimeToRemoval { get; set; }
        public string LogFile { get; set; } = "";
        public bool EnableSqlWriter { get; set; }
        public int WriterInterval { get; set; }
        public int WriterBatchSize { get; set; }
    }
}