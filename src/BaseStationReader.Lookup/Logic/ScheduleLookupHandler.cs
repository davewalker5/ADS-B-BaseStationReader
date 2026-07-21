using BaseStationReader.Api;
using BaseStationReader.BusinessLogic.Configuration;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Api;

namespace BaseStationReader.Lookup.Logic
{
    internal class ScheduleLookupHandler : LookupHandlerBase
    {
        private ISchedulesApi _api;

        /// <summary>
        /// Initialises the schedule lookup command handler.
        /// </summary>
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
            var values = Parser.GetValues(CommandLineOptionType.AirportSchedule);
            switch (values.Count)
            {
                case 1:
                    // IATA code or a file containing IATA codes
                    await HandleForTodayAsync(values);
                    break;
                case 3:
                    // IATA code or file path, followed by from and to dates
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

            // Perform the lookup and tabulate the result on the console.
            await RequestAndTabulateSchedulesAsync(values[0], from, to);
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

            // Perform the lookup and tabulate the result on the console.
            await RequestAndTabulateSchedulesAsync(values[0], from, to);
        }

        /// <summary>
        /// Request scheduling information for the single airport or for all airports listed in the file
        /// represented by the IATA code
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private async Task RequestAndTabulateSchedulesAsync(string iataCodeOrFilePath, DateTime? from, DateTime? to)
        {
            // Check the dates are valid
            if (!from.HasValue || !to.HasValue || (to <= from))
            {
                Logger.LogMessage(Severity.Error, $"Invalid time range for schedule download");
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
                    // Clean this one up and display the schedules for it.
                    var iataCode = code.Trim();
                    await RequestAndTabulateSchedulesForAirportAsync(iataCode, from, to);
                }
            }
            else
            {
                await RequestAndTabulateSchedulesForAirportAsync(iataCodeOrFilePath, from, to);
            }
        }

        /// <summary>
        /// Request scheduling information per the specified criteria and tabulate it on the console
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        private async Task RequestAndTabulateSchedulesForAirportAsync(string iata, DateTime? from, DateTime? to)
        {
            // Retrieve the provider response and convert every schedule row into displayable fields.
            var json = await _api.LookupSchedulesRawAsync(iata, from.Value, to.Value);
            var mappings = _api.ExtractFlightMapping(json, iata);
            new ScheduleTabulator().Write(iata, from.Value, to.Value, mappings);
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
