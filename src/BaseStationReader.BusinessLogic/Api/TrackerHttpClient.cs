using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api
{
    [ExcludeFromCodeCoverage]
    public sealed class TrackerHttpClient : ITrackerHttpClient
    {
        private readonly ConcurrentDictionary<ApiServiceType, int> _interCallDelay = new();
        private readonly ConcurrentDictionary<ApiServiceType, DateTime> _lastCallTimestamp = new();
        private readonly static HttpClient _client = new();
        private static TrackerHttpClient _instance = null;
        private readonly static object _lock = new();

        private TrackerHttpClient() { }

        /// <summary>
        /// Return the singleton instance of the client
        /// </summary>
        public static TrackerHttpClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new TrackerHttpClient();
                        }
                    }
                }

                return _instance;
            }
        }

        /// <summary>
        /// Set the rate limit for a given service
        /// </summary>
        /// <param name="type"></param>
        /// <param name="limit"></param>
        public void SetRateLimits(ApiServiceType type, int limit)
        {
            // Calculate the delay, in milliseconds, between calls
            _interCallDelay[type] = limit > 0 ? 1000 * (int)Math.Round(60M / limit, MidpointRounding.AwayFromZero) : 0;

            // Store a dummy "last called" timestamp that should ensure the API is called immediately the first time
            // it's used
            _lastCallTimestamp[type] = DateTime.Now.AddHours(-1);
        }

        /// <summary>
        /// Send an HTTP request and return the response
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="type"></param>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> SendAsync(ITrackerLogger logger, ApiServiceType type, HttpRequestMessage request)
        {
            // See if there's a rate limit for this API
            var delay = _interCallDelay[type];
            if (delay > 0)
            {
                logger.LogMessage(Severity.Verbose, $"Inter-call delay for {type} is {delay} ms");
    
                // There is, so find out how long ago it was last called (ms)
                var interval = (int)Math.Round((DateTime.Now - _lastCallTimestamp[type]).TotalMilliseconds, MidpointRounding.ToZero);
                var remaining = delay - interval;
                if (remaining > 0)
                {
                    logger.LogMessage(Severity.Debug, $"Waiting for {remaining} ms");
                    await Task.Delay(remaining);
                }
            }
            else
            {
                logger.LogMessage(Severity.Verbose, $"{type} does not have a rate limit");
            }

            // Send the response and capture the "last called" timestamp
                var response = await _client.SendAsync(request);
            _lastCallTimestamp[type] = DateTime.Now;

            return response;
        }
    }
}
