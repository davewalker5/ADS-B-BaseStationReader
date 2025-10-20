using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class AirlineManager : IAirlineManager
    {
        private readonly BaseStationReaderDbContext _context;

        public AirlineManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return an airline by ICAO, IATA or name, in that order
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Airline> GetAsync(string iata, string icao, string name)
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
            else if (!string.IsNullOrEmpty(name))
            {
                airline = await GetAsync(x => x.Name == name);
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
            // Clean the inputs so they're in a standardised format
            var cleanIATA = StringCleaner.CleanIATA(iata);
            var cleanICAO = StringCleaner.CleanICAO(icao);
            var cleanName = StringCleaner.CleanName(name);

            // Look for a matching record
            var airline = await GetAsync(cleanIATA, cleanICAO, cleanName);

            if (airline == null)
            {
                // No match, so create a new record
                airline = new Airline
                {
                    IATA = cleanIATA,
                    ICAO = cleanICAO,
                    Name = cleanName
                };

                await _context.Airlines.AddAsync(airline);
                await _context.SaveChangesAsync();
            }

            return airline;
        }
    }
}
