using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace BaseStationReader.Tests.Mocks
{
    public class MockFileLogger : ITrackerLogger
    {
        public void Initialise(string logFile, Severity minimumSeverityToLog, bool verbose)
        {
        }

        public void LogMessage(Severity severity, string message, [CallerMemberName] string caller = "")
        {
            Debug.Print($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [{severity.ToString()}] {caller} : {message}");
        }

        public void LogException(Exception ex, [CallerMemberName] string caller = "")
        {
            LogMessage(Severity.Error, ex.Message, caller);
            LogMessage(Severity.Error, ex.ToString(), caller);
        }

        public void LogApiConfiguration(ExternalApiSettings settings, [CallerMemberName] string caller = "")
        {
            foreach (var service in settings.ApiServices)
            {
                LogMessage(Severity.Debug, service.ToString());
                foreach (var endpoint in service.ApiEndpoints)
                {
                    LogMessage(Severity.Debug, $"{service.Service} API : {endpoint}", caller);
                }
            }
        }
    }
}
