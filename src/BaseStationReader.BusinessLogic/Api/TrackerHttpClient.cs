using BaseStationReader.Entities.Interfaces;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.BusinessLogic.Api
{
    [ExcludeFromCodeCoverage]
    public sealed class TrackerHttpClient : ITrackerHttpClient
    {
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
        /// Send an HTTP request and return the response
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request)
            => await _client.SendAsync(request);
    }
}
