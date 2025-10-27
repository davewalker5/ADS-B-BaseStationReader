using BaseStationReader.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Api;
using System.Text.Json;

namespace BaseStationReader.Lookup.Logic
{
    internal class ScheduleLookupHandler : LookupHandlerBase
    {
        private ISchedulesApi _api;

        private readonly JsonSerializerOptions _options = new()
        {
            WriteIndented = true
        };

        public ScheduleLookupHandler(
            LookupToolApplicationSettings settings,
            LookupToolCommandLineParser parser,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            IExternalApiFactory apiFactory) : base(settings, parser, logger, factory, apiFactory)
        {
        }

        public async Task HandleAsync()
        {
            // Extract the lookup parameters from the command line
            var values = Parser.GetValues(CommandLineOptionType.ExportSchedule);
            switch (values.Count)
            {
                case 2:
                    // IATA code and output folder
                    await HandleForTodayAsync(values);
                    break;
                case 4:
                    // IATA code, from date, to date and output folder
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
        private async Task HandleForTodayAsync(IList<string> values)
        {
            // Use the times in the settings file to configure start and end date and time objects for today
            var from = GetScheduleTime(Settings.ScheduleStartTime);
            var to = GetScheduleTime(Settings.ScheduleEndTime);

            // Perform the lookup and export the result to a JSON file
            await RequestAndExportSchedulesAsync(values[0], from, to, values[1]);
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

            // Perform the lookup and export the result to a JSON file
            await RequestAndExportSchedulesAsync(values[0], from, to, values[3]);
        }

        /// <summary>
        /// Request scheduling information for the single airport or for all airports listed in the file
        /// represented by the IATA code
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="outputFolder"></param>
        /// <returns></returns>
        private async Task RequestAndExportSchedulesAsync(string iataCodeOrFilePath, DateTime? from, DateTime? to, string outputFolder)
        {
            // Check the dates are valid
            if (!from.HasValue || !to.HasValue || (to <= from))
            {
                Logger.LogMessage(Severity.Error, $"Invalid time range for schedule download");
                return;
            }

            // Check the output folder exists
            if (!Path.Exists(outputFolder))
            {
                Logger.LogMessage(Severity.Error, $"Output folder {outputFolder} does not exist");
                return;
            }

            // Construct the API instance
            _api = ApiFactory.GetApiInstance(ApiServiceType.AeroDataBox, ApiEndpointType.Schedules, TrackerHttpClient.Instance, Factory, Settings) as ISchedulesApi;
            if (_api == null)
            {
                Logger.LogMessage(Severity.Error, $"API instance is not a schedule retrieval API");
                return;
            }

            // Is it a single code or a file path?
            if (File.Exists(iataCodeOrFilePath))
            {
                // File path, so read the content and iterate over each code
                var codes = File.ReadAllLines(iataCodeOrFilePath);
                foreach (var code in codes)
                {
                    // Clean this one up and download the schedules for it
                    var iataCode = code.Trim();
                    await RequestAndExportSchedulesForAirportAsync(iataCode, from, to, outputFolder);
                }
            }
            else
            {
                await RequestAndExportSchedulesForAirportAsync(iataCodeOrFilePath, from, to, outputFolder);
            }
        }

        /// <summary>
        /// Request scheduling information per the specified criteria and export to a JSON file
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <param name="outputFolder"></param>
        /// <returns></returns>
        private async Task RequestAndExportSchedulesForAirportAsync(string iata, DateTime? from, DateTime? to, string outputFolder)
        {
            // Construct the output file name from the IATA code and "from" date
            var prefix = from.Value.ToString("yyyy-MM-dd");
            var filePath = Path.Join(outputFolder, $"{prefix}-{iata}.json");

            // Perform the lookup
            var json = await _api.LookupSchedulesRawAsync(iata, from.Value, to.Value);
            if (json != null)
            {
                // Write the JSON to the specified output file
                File.WriteAllText(filePath, json.ToJsonString(_options));
            }
        }
        
        /// <summary>
        /// Given a time in HH:MM format from the settings file, try to construct that time today as a date and
        /// time object
        /// </summary>
        /// <param name="timeString"></param>
        /// <returns></returns>
        private DateTime? GetScheduleTime(string timeString)
        {
            var dateString = DateTime.Today.ToString("yyyy-MMM-dd");
            var dateTimeString = $"{dateString} {timeString}";

            if (!DateTime.TryParse(dateTimeString, out DateTime dateTime))
            {
                Logger.LogMessage(Severity.Error, $"{dateTimeString} is not a valid date and time");
                return null;
            }

            return dateTime;
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