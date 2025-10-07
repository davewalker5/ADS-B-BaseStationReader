using BaseStationReader.Entities.Api;

namespace BaseStationReader.Interfaces.Api
{
    public interface IAirlineApiWrapper
    {
        Task<Airline> LookupAirlineAsync(string icao, string iata, string name);
    }
}