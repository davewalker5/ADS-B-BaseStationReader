using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.SkyLink
{
    internal class SkyLinkActiveFlightApi : SkyLinkFlightApiBase, IActiveFlightsApi
    {
        public SkyLinkActiveFlightApi(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(ApiEndpointType.ActiveFlights, logger, client, factory, settings)
        {
        }

        /// <summary>
        /// Look up a flight given the flight number
        /// </summary>
        /// <param name="_"></param>
        /// <param name="flightNumber"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Dictionary<ApiProperty, string>> LookupFlightAsync(ApiProperty _, string flightNumber)
            => await LookupFlightByNumberAsync(flightNumber);

        /// <summary>
        /// The SkyLink API doesn't implement the flights in bounding box endpoint
        /// </summary>
        /// <param name="centreLatitude"></param>
        /// <param name="centreLongitude"></param>
        /// <param name="rangeNm"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
#pragma warning disable CS1998
        public async Task<List<Dictionary<ApiProperty, string>>> LookupFlightsInBoundingBoxAsync(double centreLatitude, double centreLongitude, double rangeNm)
            => null;
#pragma warning restore CS1998
    }
}