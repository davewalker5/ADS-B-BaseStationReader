using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;

namespace BaseStationReader.Tests.Mocks
{
    public class MockFileLogger : ITrackerLogger
    {
        public void Initialise(string logFile)
        {
        }

        public void LogMessage(Severity severity, string message)
        {
        }

        public void LogException(Exception ex)
        {

        }
    }
}
