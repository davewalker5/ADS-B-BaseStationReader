using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Entities.Interfaces
{
    public interface ITrackerLogger
    {
        void Initialise(string logFile, Severity minimumSeverityToLog);
        void LogMessage(Severity severity, string message);
        void LogException(Exception ex);
    }
}
