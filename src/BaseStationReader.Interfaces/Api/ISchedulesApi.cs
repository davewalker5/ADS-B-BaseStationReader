using System.Text.Json.Nodes;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface ISchedulesApi : IExternalApi
    {
        /// <summary>
        /// Looks up raw scheduling information for an airport in a time range.
        /// </summary>
        Task<JsonNode> LookupSchedulesRawAsync(string iata, DateTime from, DateTime to);

        /// <summary>
        /// Extracts flight IATA code mappings from an airport schedule response.
        /// </summary>
        List<FlightScheduleEntry> ExtractFlightMapping(JsonNode schedules, string airportIata);
    }
}
