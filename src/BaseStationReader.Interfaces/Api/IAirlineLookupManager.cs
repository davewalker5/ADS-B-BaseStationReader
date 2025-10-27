using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IAirlineLookupManager
    {
        Task<Airline> IdentifyAirlineAsync(string icao, string iata, string name);
    }
}