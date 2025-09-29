using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Entities.Config
{
    [ExcludeFromCodeCoverage]
    public class ApiService
    {
        public ApiServiceType Service { get; set; }
        public string Key { get; set; } = "";

        /// <summary>
        /// The rate limit is the maximum number of requests per minute to the service as a whole,
        /// irrespective of endpoint
        /// </summary>
        public int RateLimit { get; set; } = 0;

        public override string ToString()
            => $"{Service} : Key = {Key}";
    }
}
