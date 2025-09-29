using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using System.Net;

namespace BaseStationReader.Tests.Mocks
{
    internal class MockTrackerHttpClient : ITrackerHttpClient
    {
        private readonly Queue<string> _responses = new();

        /// <summary>
        /// Queue a response
        /// </summary>
        /// <param name="response"></param>
        public void AddResponse(string response)
        {
            _responses.Enqueue(response);
        }

        /// <summary>
        /// Queue a set of responses
        /// </summary>
        /// <param name="responses"></param>
        public void AddResponses(IEnumerable<string> responses)
        {
            foreach (string response in responses)
            {
                _responses.Enqueue(response);
            }
        }

        /// <summary>
        /// Set the rate limit for a given service
        /// </summary>
        /// <param name="type"></param>
        /// <param name="limit"></param>
        public void SetRateLimits(ApiServiceType type, int limit)
        {
            
        }

        /// <summary>
        /// Construct and return the next response
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="type"></param>
        /// <param name="message"></param>
        /// <returns></returns>
#pragma warning disable CS1998
        public async Task<HttpResponseMessage> SendAsync(ITrackerLogger logger, ApiServiceType type, HttpRequestMessage message)
        {
            // De-queue the next message
            var content = _responses.Dequeue();

            // If the content is null, raise an exception to test the exception handling
            if (content == null)
            {
                throw new Exception();
            }

            // Construct an HTTP response
            var response = new HttpResponseMessage
            {
                Content = new StringContent(content ?? ""),
                StatusCode = HttpStatusCode.OK
            };

            return response;
        }
#pragma warning restore CS1998
    }
}
