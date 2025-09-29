using BaseStationReader.Data;
using BaseStationReader.Entities.Lookup;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    public class AirlineManager : IAirlineManager
    {
        private readonly BaseStationReaderDbContext _context;

        public AirlineManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return an airline by either ICAO or IATA code, whichever is specified
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<Airline> GetByCodeAsync(string iata, string icao)
        {
            Airline airline = null;

            if (!string.IsNullOrEmpty(icao))
            {
                airline = await GetAsync(x => x.ICAO == icao);
            }
            else if (!string.IsNullOrEmpty(iata))
            {
                airline = await GetAsync(x => x.IATA == iata);
            }

            return airline;
        }

        /// <summary>
        /// Return the first airline matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<Airline> GetAsync(Expression<Func<Airline, bool>> predicate)
        {
            List<Airline> airlines = await ListAsync(predicate);
            return airlines.FirstOrDefault();
        }


        /// <summary>
        /// Return all airlines matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<Airline>> ListAsync(Expression<Func<Airline, bool>> predicate)
            => await _context.Airlines.Where(predicate).ToListAsync();

        /// <summary>
        /// Add an airline, if it doesn't already exist
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Airline> AddAsync(string iata, string icao, string name)
        {
            var airline = await GetByCodeAsync(iata, icao);
            if (airline == null)
            {
                airline = new Airline { IATA = iata, ICAO = icao, Name = name };
                await _context.Airlines.AddAsync(airline);
                await _context.SaveChangesAsync();
            }

            return airline;
        }
    }
}
