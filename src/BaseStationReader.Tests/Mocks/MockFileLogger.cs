using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Tests.Mocks
{
    [ExcludeFromCodeCoverage]
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
