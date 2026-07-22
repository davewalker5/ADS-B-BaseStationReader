using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class AirportManager : IAirportManager
    {
        private readonly BaseStationReaderDbContext _context;

        /// <summary>
        /// Initialise a manager using the supplied database context.
        /// </summary>
        public AirportManager(BaseStationReaderDbContext context)
            => _context = context;

        /// <summary>
        /// Return an airport by ICAO, IATA or name, in that order.
        /// </summary>
        public async Task<Airport> GetAsync(string iata, string icao, string name)
        {
            Airport airport = null;

            if (!string.IsNullOrEmpty(icao))
            {
                airport = await GetAsync(x => x.ICAO == icao);
            }
            else if (!string.IsNullOrEmpty(iata))
            {
                airport = await GetAsync(x => x.IATA == iata);
            }
            else if (!string.IsNullOrEmpty(name))
            {
                airport = await GetAsync(x => x.Name == name);
            }

            return airport;
        }

        /// <summary>
        /// Return the first airport matching the specified criteria.
        /// </summary>
        public async Task<Airport> GetAsync(Expression<Func<Airport, bool>> predicate)
        {
            List<Airport> airports = await ListAsync(predicate);
            return airports.FirstOrDefault();
        }

        /// <summary>
        /// Return all airports matching the specified criteria.
        /// </summary>
        public async Task<List<Airport>> ListAsync(Expression<Func<Airport, bool>> predicate)
            => await _context.Airports.Include(x => x.Provenance).Where(predicate).ToListAsync();

        /// <summary>
        /// Add an airport if it does not already exist.
        /// </summary>
        public async Task<Airport> AddAsync(Airport airport)
        {
            ArgumentNullException.ThrowIfNull(airport);

            // Clean identifying values so lookups and imported records use a consistent format.
            airport.IATA = StringCleaner.CleanIATA(airport.IATA);
            airport.ICAO = StringCleaner.CleanICAO(airport.ICAO);
            airport.Name = StringCleaner.CleanName(airport.Name);
            if (!await _context.Provenance.AnyAsync(x => x.Id == airport.ProvenanceId))
                throw new InvalidOperationException($"Provenance record {airport.ProvenanceId} does not exist.");

            var existing = await GetAsync(airport.IATA, airport.ICAO, airport.Name);
            if (existing == null)
            {
                await _context.Airports.AddAsync(airport);
                await _context.SaveChangesAsync();
                existing = airport;
            }

            return existing;
        }
    }
}
