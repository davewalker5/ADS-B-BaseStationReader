﻿using BaseStationReader.Entities.Logging;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class TrackerApplicationSettings
    {
        public Severity MinimumLogLevel { get; set; }
        public string Host { get; set; } = "";
        public int Port { get; set; }
        public int SocketReadTimeout { get; set; }
        public int ApplicationTimeout { get; set; }
        public bool RestartOnTimeout { get; set; }
        public int TimeToRecent { get; set; }
        public int TimeToStale { get; set; }
        public int TimeToRemoval { get; set; }
        public int TimeToLock { get; set; }
        public int RefreshInterval { get; set; }
        public string LogFile { get; set; } = "";
        public bool EnableSqlWriter { get; set; }
        public int WriterInterval { get; set; }
        public int WriterBatchSize { get; set; }
        public int MaximumRows { get; set; }
        public double? ReceiverLatitude { get; set; }
        public double? ReceiverLongitude { get; set; }
        public List<ApiEndpoint> ApiEndpoints { get; set; } = new List<ApiEndpoint>();
        public List<ApiServiceKey> ApiServiceKeys { get; set; } = new List<ApiServiceKey>();
        public List<TrackerColumn> Columns { get; set; } = new List<TrackerColumn>();
    }
}