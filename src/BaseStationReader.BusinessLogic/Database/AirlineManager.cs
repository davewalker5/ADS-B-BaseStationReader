using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

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
        /// Return the first airline matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<Airline> GetAsync(Expression<Func<Airline, bool>> predicate)
        {
            List<Airline> airlines = await ListAsync(predicate);

#pragma warning disable CS8603
            return airlines.FirstOrDefault();
#pragma warning restore CS8603
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
            var airline = await GetAsync(a => (a.IATA == iata) || (a.ICAO == icao) || (a.Name == name));

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
