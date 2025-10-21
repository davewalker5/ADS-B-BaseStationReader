using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Api.SkyLink
{
    internal class SkyLinkActiveFlightApi : SkyLinkFlightApiBase, IActiveFlightsApi
    {
        public SkyLinkActiveFlightApi(
            ITrackerHttpClient client,
            IDatabaseManagementFactory factory,
            ExternalApiSettings settings) : base(ApiEndpointType.ActiveFlights, client, factory, settings)
        {
        }

        /// <summary>
        /// Look up a flight given the flight IATA code
        /// </summary>
        /// <param name="_"></param>
        /// <param name="flightIATA"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<Dictionary<ApiProperty, string>> LookupFlightAsync(ApiProperty _, string flightIATA)
            => await LookupFlightByNumberAsync(ApiEndpointType.ActiveFlights, flightIATA);
    }
}