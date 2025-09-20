using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;

namespace BaseStationReader.BusinessLogic.Database
{
    public class FlightManager : IFlightManager
    {
        private readonly BaseStationReaderDbContext _context;

        public FlightManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Get the first flight matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<Flight> GetAsync(Expression<Func<Flight, bool>> predicate)
        {
            List<Flight> models = await ListAsync(predicate);
            return models.FirstOrDefault();
        }

        /// <summary>
        /// Return all flights matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<Flight>> ListAsync(Expression<Func<Flight, bool>> predicate)
            => await _context
                .Flights
                .Where(predicate)
                .Include(x => x.Airline)
                .ToListAsync();

        /// <summary>
        /// Add a new flight to the database
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public async Task<Flight> AddAsync(
            string iata,
            string icao,
            string number,
            string embarkation,
            string destination,
            int airlineId)
        {
            var model = await GetAsync(x => (x.IATA == iata) || (x.ICAO == icao));
            if (model == null)
            {
                model = new Flight
                {
                    Number = number,
                    IATA = iata,
                    ICAO = icao,
                    Embarkation = embarkation,
                    Destination = destination,
                    AirlineId = airlineId
                };
                await _context.Flights.AddAsync(model);
                await _context.SaveChangesAsync();
            }

            return model;
        }
    }
}
