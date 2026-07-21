using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    /// <summary>
    /// Coordinates schedule retrieval and conversion into flight IATA code mappings.
    /// </summary>
    public interface IScheduleLookupManager
    {
        /// <summary>
        /// Retrieves an airport schedule and extracts its flight IATA code mappings.
        /// </summary>
        Task<List<FlightIATACodeMapping>> LookupSchedulesAsync(string iata, DateTime from, DateTime to);
    }
}
