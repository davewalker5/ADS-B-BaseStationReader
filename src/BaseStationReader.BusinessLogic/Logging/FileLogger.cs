using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using Serilog;
using Serilog.Core;
using Serilog.Events;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Interfaces.Logging;
using System.Diagnostics;

namespace BaseStationReader.BusinessLogic.Logging
{
    [ExcludeFromCodeCoverage]
    public class FileLogger : ITrackerLogger
    {
        private static readonly string _loggerTypeName = typeof(FileLogger).Name;
        private static readonly string _loggerTypeNamespacePrefix = typeof(FileLogger).Namespace.Split(".")[0];

        private bool _configured = false;
        private bool _verbose = false;

        /// <summary>
        /// Configure logging using Serilog
        /// </summary>
        /// <param name="logFile"></param>
        /// <param name="minimumSeverityToLog"></param>
        /// <param name="verbose"></param>
        public void Initialise(string logFile, Severity minimumSeverityToLog, bool verbose)
        {
            // If the log file's empty, return now without configuring a logger
            if (string.IsNullOrEmpty(logFile))
            {
                return;
            }

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
                    rollOnFileSizeLimit: true,
                    buffered: false)
                .CreateLogger();
#pragma warning restore CS8602, S4792

            // Set the "configured" flag and store the verbose logging level flag
            _configured = true;
            _verbose = verbose;
        }

        /// <summary>
        /// Log a message with the specified severity
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="message"></param>
        /// <param name="caller"></param>
        public void LogMessage(Severity severity, string message, string caller = null)
        {
            // Check the logger is configured
            if (!_configured) return;

            // Add the caller to the message
            caller = GetCallerDetails();
            var traceableMessage = !string.IsNullOrEmpty(caller) ? $"{caller} : {message}" : message;

            // Log the message
            switch (severity)
            {
                case Severity.Debug:
                    Log.Debug(traceableMessage);
                    break;
                case Severity.Info:
                    Log.Information(traceableMessage);
                    break;
                case Severity.Warning:
                    Log.Warning(traceableMessage);
                    break;
                case Severity.Error:
                    Log.Error(traceableMessage);
                    break;
                case Severity.Verbose:
                    if (_verbose)
                    {
                        Log.Debug(traceableMessage);
                    }
                    break;
                default:
                    break;
            }
        }

        /// <summary>
        /// Log exception details, including the stack trace
        /// </summary>
        /// <param name="ex"></param>
        /// <param name="caller"></param>
        public void LogException(Exception ex, string caller = "")
        {
            // Check the logger is configured
            if (!_configured) return;

            // Get the calling method details
            caller = GetCallerDetails();

            LogMessage(Severity.Error, ex.Message, caller);
            LogMessage(Severity.Error, ex.ToString(), caller);
        }

        /// <summary>
        /// Log external API configuration details
        /// </summary>
        /// <param name=""></param>
        public void LogApiConfiguration(ExternalApiSettings settings, string caller = "")
        {
            // Check the logger is configured
            if (!_configured) return;

            // Get the calling method details
            caller = GetCallerDetails();

            // Iterate over the service definitions
            foreach (var service in settings.ApiServices)
            {
                // Log the definition for the current service
                LogMessage(Severity.Debug, service.ToString(), caller);

                // Iterate over the service endpoints and log each one
                foreach (var endpoint in settings.ApiEndpoints.Where(x => x.Service == service.Service))
                {
                    LogMessage(Severity.Debug, endpoint.ToString(), caller);
                }
            }
        }

        /// <summary>
        /// Walk the stack to determine the caller's declaring type and method name details (the
        /// [CallerMemberName] attribute doesn't include the declaring type name)
        /// </summary>
        private static string GetCallerDetails()
        {
            // Get the current stack trace and iterate over the frames
            var stack = new StackTrace();
            foreach (var frame in stack.GetFrames())
            {
                // Get the method and declaring type details from the current frame
                var method = frame.GetMethod();
                var declaringType = method.DeclaringType;
                if (declaringType == null) continue;

                // Get the name and namespace for the type
                var declaringTypeName = declaringType.Name;
                var declaringTypeNamespace = declaringType.Namespace;

                // Move on to the next frame if:
                // 
                // 1. The declaring type name is empty
                // 2. It's another method in the current class
                // 3. It's a compiler generated method
                // 4. It's not in the application namespace
                //
                if (string.IsNullOrEmpty(declaringTypeName) ||
                    declaringTypeName.Equals(_loggerTypeName) ||
                    declaringTypeName.Contains("<") ||
                    !declaringTypeNamespace.StartsWith(_loggerTypeNamespacePrefix))
                {
                    continue;
                }

                // Found the calling type and method - return the details
                return $"{declaringTypeName}.{method.Name}";
            }

            return "";
        }
    }
}
