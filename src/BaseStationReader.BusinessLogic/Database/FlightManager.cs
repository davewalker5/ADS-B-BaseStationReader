using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using Microsoft.EntityFrameworkCore;
using System.Linq.Expressions;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class FlightManager : IFlightManager
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
            List<Flight> flights = await ListAsync(predicate);
            return flights.FirstOrDefault();
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
            string embarkation,
            string destination,
            int airlineId)
        {
            // Check the flight doesn't exist based on the airline, number and route
            var flight = await GetAsync(x =>
                (x.AirlineId == airlineId) &&
                (x.IATA == iata) &&
                (x.Embarkation == embarkation) &&
                (x.Destination == destination));

            if (flight == null)
            {
                flight = new Flight
                {
                    IATA = iata,
                    ICAO = icao,
                    Embarkation = embarkation,
                    Destination = destination,
                    AirlineId = airlineId
                };

                await _context.Flights.AddAsync(flight);
                await _context.SaveChangesAsync();
            }

            return flight;
        }
    }
}
