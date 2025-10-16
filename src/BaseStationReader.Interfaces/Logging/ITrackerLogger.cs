using System.Runtime.CompilerServices;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Interfaces.Logging
{
    public interface ITrackerLogger
    {
        void Initialise(string logFile, Severity minimumSeverityToLog, bool verbose);
        void LogMessage(Severity severity, string message, [CallerMemberName] string caller = "");
        void LogException(Exception ex, [CallerMemberName] string caller = "");
        void LogApiConfiguration(ExternalApiSettings settings, [CallerMemberName] string caller = "");
    }
}
