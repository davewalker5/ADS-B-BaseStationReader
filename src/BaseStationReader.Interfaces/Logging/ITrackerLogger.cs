using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Interfaces.Logging
{
    public interface ITrackerLogger
    {
        void Initialise(string logFile, Severity minimumSeverityToLog, bool verbose);
        void LogMessage(Severity severity, string message);
        void LogException(Exception ex);
        void LogApiConfiguration(ExternalApiSettings settings);
    }
}
