using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Tests.Mocks
{
    [ExcludeFromCodeCoverage]
    public class MockFileLogger : ITrackerLogger
    {
        public void Initialise(string logFile, Severity minimumSeverityToLog)
        {
        }

        public void LogMessage(Severity severity, string message)
        {
            Debug.Print($"{DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss")} [{severity.ToString()}] {message}");
        }

        public void LogException(Exception ex)
        {
            LogMessage(Severity.Error, ex.Message);
            LogMessage(Severity.Error, ex.ToString());
        }

        public void LogApiConfiguration(ExternalApiSettings settings)
        {
            foreach (var service in settings.ApiServiceKeys)
            {
                LogMessage(Severity.Debug, service.ToString());
                foreach (var endpoint in settings.ApiEndpoints.Where(x => x.Service == service.Service))
                {
                    LogMessage(Severity.Debug, endpoint.ToString());
                }
            }
        }
    }
}
