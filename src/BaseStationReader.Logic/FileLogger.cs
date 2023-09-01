using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Logic
{
    [ExcludeFromCodeCoverage]
    public class FileLogger : ITrackerLogger
    {
        /// <summary>
        /// Configure logging using Serilog
        /// </summary>
        /// <param name="logFile"></param>
        /// <param name="minimumSeverityToLog"></param>
        public void Initialise(string logFile, Severity minimumSeverityToLog)
        {
            // Set the minimum log level
            var levelSwitch = new LoggingLevelSwitch();
            switch (minimumSeverityToLog)
            {
                case Severity.Debug:
                    levelSwitch.MinimumLevel = LogEventLevel.Debug;
                    break;
                case Severity.Info:
                    levelSwitch.MinimumLevel = LogEventLevel.Information;
                    break;
                case Severity.Warning:
                    levelSwitch.MinimumLevel = LogEventLevel.Warning;
                    break;
                case Severity.Error:
                    levelSwitch.MinimumLevel = LogEventLevel.Error;
                    break;
                default:
                    break;
            }

            // Configure the logger
#pragma warning disable CS8602, S4792
            Log.Logger = new LoggerConfiguration()
            .MinimumLevel.ControlledBy(levelSwitch)
            .WriteTo
            .File(
                    logFile,
                    rollingInterval: RollingInterval.Day,
                    rollOnFileSizeLimit: true)
                .CreateLogger();
#pragma warning restore CS8602, S4792
        }

        /// <summary>
        /// Log a message with the specified severity
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        public void LogMessage(Severity severity, string message)
        {
            switch (severity)
            {
                case Severity.Debug:
                    Log.Debug(message);
                    break;
                case Severity.Info:
                    Log.Information(message);
                    break;
                case Severity.Warning:
                    Log.Warning(message);
                    break;
                case Severity.Error:
                    Log.Error(message);
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Log exception details, including the stack trace
        /// </summary>
        /// <param name="ex"></param>
        public void LogException(Exception ex)
        {
            Log.Error(ex.Message);
            Log.Error(ex.ToString());
        }
    }
}
