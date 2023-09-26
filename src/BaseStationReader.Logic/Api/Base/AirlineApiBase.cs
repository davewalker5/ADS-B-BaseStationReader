using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Logic.Api.Base
{
    [ExcludeFromCodeCoverage]
    public abstract class AirlineApiBase
    {
        private readonly IAirlineManager _manager;

        protected AirlineApiBase(IAirlineManager manager)
        {
            _manager = manager;
        }

        /// <summary>
        /// Lookup an airline using its IATA code
        /// </summary>
        /// <param name="iata"></param>
        /// <returns></returns>
        public async virtual Task<Airline?> LookupAirlineByIATACode(string iata)
        {
            var airline = await _manager.GetAsync(x => x.IATA == iata);
            return airline;
        }

        /// <summary>
        /// Lookup an airline using it's ICAO code
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async virtual Task<Airline?> LookupAirlineByICAOCode(string icao)
        {
            var airline = await _manager.GetAsync(x => x.ICAO == icao);
            return airline;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        protected async Task<Airline> WriteAirline(string iata, string icao, string name)
        {
            var airline = await _manager.AddAsync(iata, icao, name);
            return airline;
        }
    }
}
