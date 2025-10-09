using BaseStationReader.BusinessLogic.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.BusinessLogic.Api.Wrapper;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Api;
using System.Text.Json;

namespace BaseStationReader.Lookup.Logic
{
    internal class ScheduleLookupHandler : LookupHandlerBase
    {
        private readonly ApiServiceType _serviceType;

        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true
        };

        public ScheduleLookupHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            ApiServiceType serviceType) : base(settings, parser, logger, factory)
        {
            _serviceType = serviceType;
        }

        public async Task HandleAsync()
        {
            Logger.LogMessage(Severity.Info, $"Using the {_serviceType} API");

            // Extract the lookup parameters from the command line
            var values = Parser.GetValues(CommandLineOptionType.ExportSchedule);
            switch (values.Count)
            {
                case 1:
                case 2:
                    // IATA code and output file path
                    await HandleForNowAsync(values);
                    break;
                case 3:
                case 4:
                    // IATA code, from date, to date and output file path
                    await HandleForDateRangeAsync(values);
                    break;
                default:
                    // Invalid command line values
                    Logger.LogMessage(Severity.Error, $"Invalid command line arguments for the schedule lookup command");
                    break;
            }
        }

        /// <summary>
        /// Handle the schedule lookup command for a given airport and a date range spanning "now"
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private async Task HandleForNowAsync(IList<string> values)
        {
            // Set the "from" date to an hour before now, so the date range spans the current time,
            // and the "to" date to 11 hours ahead
            var from = DateTime.Now.AddHours(-1);
            var to = DateTime.Now.AddHours(11);
            var filePath = values.Count == 2 ? values[1] : null;

            // Perform the lookup and export the result to a JSON file
            await RequestAndExportSchedulesAsync(values[0], filePath, from, to);
        }

        /// <summary>
        /// Handle the schedule lookup command for an airport and date range
        /// </summary>
        /// <param name="values"></param>
        /// <returns></returns>
        private async Task HandleForDateRangeAsync(IList<string> values)
        {
            // Parse the string representations of the dates to yield date/time objects
            var from = ExtractTimestamp(values[1]);
            var to = ExtractTimestamp(values[2]);
            var filePath = values.Count == 4 ? values[3] : null;

            // Perform the lookup and export the result to a JSON file
            await RequestAndExportSchedulesAsync(values[0], filePath, from, to);
        }

        /// <summary>
        /// Request scheduling information per the specified criteria and export to a JSON file
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="filePath"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private async Task RequestAndExportSchedulesAsync(string iata, string filePath, DateTime? from, DateTime? to)
        {
            // Check the dates are valid
            if (!from.HasValue || !to.HasValue)
            {
                return;
            }

            // If the file path isn't specified, construct it from the IATA code and "from" date
            if (string.IsNullOrEmpty(filePath))
            {
                var prefix = from.Value.ToString("yyyy-MM-dd");
                filePath = $"{prefix}-{iata}.json";
            }

            // Get an instance of the API
                var instance = ExternalApiFactory.GetApiInstance(_serviceType, ApiEndpointType.Schedules, Logger, TrackerHttpClient.Instance, Factory, Settings);
            if (instance is ISchedulesApi api)
            {
                // Perform the lookup
                var json = await api.LookupSchedulesRawAsync(iata, from.Value, to.Value);
                if (json != null)
                {
                    // Write the JSON to the specified output file
                    File.WriteAllText(filePath, json.ToJsonString(_options));
                }
            }
        }

        /// <summary>
        /// Extract a date and time from a string representation
        /// </summary>
        /// <param name="dateTimeString"></param>
        /// <returns></returns>
        private DateTime? ExtractTimestamp(string dateTimeString)
        {
            if (!DateTime.TryParse(dateTimeString, out DateTime timestamp))
            {
                Logger.LogMessage(Severity.Error, $"{dateTimeString} is not a valid date and time");
                return null;
            }

            return timestamp;
        }
    }
}