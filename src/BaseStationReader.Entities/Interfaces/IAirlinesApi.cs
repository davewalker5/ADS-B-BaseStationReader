using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.Entities.Interfaces
{
    public interface IAirlinesApi
    {
        Task<Airline?> LookupAirlineByIATACode(string iata);
        Task<Airline?> LookupAirlineByICAOCode(string icao);
    }
}