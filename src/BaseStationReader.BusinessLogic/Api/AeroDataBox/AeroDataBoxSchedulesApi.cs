using System.Diagnostics.CodeAnalysis;
using System.Text.Json.Nodes;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.AeroDatabox
{
    internal class AeroDataBoxSchedulesApi : ExternalApiBase, ISchedulesApi
    {
        private const ApiServiceType ServiceType = ApiServiceType.AeroDataBox;
        private const string DateTimeFormat = "yyyy-MM-ddTHH:mm";
        private const int TimespanLimitHours = 12;

        private readonly string _baseAddress;
        private readonly string _host;
        private readonly string _key;

        private static readonly Dictionary<string, string> _queryParameters = new()
        {
            { "withLeg", "false" },
            { "direction", "both" },
            { "withCancelled", "true" },
            { "withCodeshared", "true" },
            { "withCargo", "true" },
            { "withPrivate", "false" },
            { "withLocation", "false" },
            { "codeType", "iata" }
        };

        [ExcludeFromCodeCoverage]
        public AeroDataBoxSchedulesApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(logger, client, factory)
        {
            // Get the API configuration properties and store the key
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);
            _key = definition?.Key;

            // Get the endpoint URL, set up the base address for requests and extract the host name
            var url = settings.ApiEndpoints.FirstOrDefault(x => x.EndpointType == ApiEndpointType.Schedules && x.Service == ServiceType)?.Url;
            _baseAddress = url;
            _host = new Uri(url).Host;

            // Set the rate limit for this service on the HTTP client
            Logger.LogMessage(Severity.Info, $"Using rate limit of {definition?.RateLimit}");
            client.SetRateLimits(ServiceType, definition?.RateLimit ?? 0);
        }

        /// <summary>
        /// Lookup and return JSON scheduling information for an airport in a time range
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="from"></param>
        /// <param name="to"></param>
        /// <returns></returns>
        public async Task<JsonNode> LookupSchedulesRawAsync(string iata, DateTime from, DateTime to)
        {
            // Make sure the timespan represented by the two dates is valid
            var timespan = (int)Math.Round((to - from).TotalMinutes, 0, MidpointRounding.AwayFromZero);
            if (timespan < 1)
            {
                Logger.LogMessage(Severity.Error, $"{from} to {to} gives a timespan of {timespan} minutes which is invalid");
                return null;
            }

            // Make sure the timespan represented by the two dates is within the allowed range
            if (timespan > 60 * TimespanLimitHours)
            {
                Logger.LogMessage(Severity.Error, $"{from} to {to} gives a timespan of {timespan} minutes which is too large");
                return null;
            }

            // Construct date representations of the dates
            var fromStr = from.ToString(DateTimeFormat);
            var toStr = to.ToString(DateTimeFormat);

            // Make the request and return the resulting JSON node
            var schedules = await MakeApiRequestAsync($"{iata}/{fromStr}/{toStr}");
            return schedules;
        }

        /// <summary>
        /// Make a request to the specified URL
        /// </summary>
        /// <param name="parameters"></param>
        /// <returns></returns>
        private async Task<JsonNode> MakeApiRequestAsync(string parameters)
        {
            JsonNode node = null;

            try
            {
                // Construct the URL with query parameters
                var url = $"{_baseAddress}/{parameters}?{BuildQueryString()}";

                // Make a request for the data from the API
                node = await GetAsync(Logger, ServiceType, url, new Dictionary<string, string>()
                {
                    { "X-RapidAPI-Key", _key },
                    { "X-RapidAPI-Host", _host },
                });
            }
            catch (Exception ex)
            {
                Logger.LogMessage(Severity.Error, ex.Message);
                Logger.LogException(ex);
            }

            return node;
        }

        /// <summary>
        /// Build the query string from the fixed parameters
        /// </summary>
        /// <returns></returns>
        private static string BuildQueryString()
            => string.Join("&", _queryParameters.Select(x => $"{x.Key}={x.Value}"));
    }
}