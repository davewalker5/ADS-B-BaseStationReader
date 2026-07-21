using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Tracking;

namespace BaseStationReader.Interfaces.Api
{
    public interface IExternalApiWrapper
    {
        /// <summary>
        /// Resolves an aircraft from local data or the configured aircraft API.
        /// </summary>
        Task<Aircraft> LookupAircraftAsync(string address);

        /// <summary>
        /// Resolves a flight from local data or the configured flights API.
        /// </summary>
        Task<Flight> LookupFlightAsync(string address, string callsign);

        Task<LookupResult> LookupAsync(ApiLookupRequest request);
        Task<IEnumerable<string>> LookupCurrentAirportWeatherAsync(string icao);
        Task<IEnumerable<string>> LookupAirportWeatherForecastAsync(string icao);
        /// <summary>
        /// Retrieves an airport schedule and extracts its flight IATA code mappings.
        /// </summary>
        Task<List<FlightIATACodeMapping>> LookupSchedulesAsync(string iata, DateTime from, DateTime to);
    }
}
