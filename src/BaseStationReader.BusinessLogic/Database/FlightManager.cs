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
                .Include(x => x.Provenance)
                .ToListAsync();

        /// <summary>
        /// Add a new flight to the database
        /// </summary>
        /// <param name="template"></param>
        /// <returns></returns>
        public async Task<Flight> AddAsync(
            string iata,
            string icao,
            string callsign,
            string embarkation,
            string destination,
            int airlineId,
            int provenanceId = 0)
        {
            if (string.IsNullOrWhiteSpace(callsign))
                throw new ArgumentException("A callsign is required.", nameof(callsign));

            callsign = callsign.Trim().ToUpperInvariant();

            if (provenanceId == 0)
            {
                var local = await _context.Provenance.FirstOrDefaultAsync(x => x.SourceRef == "LOCAL");
                if (local == null)
                {
                    local = new Provenance
                    {
                        SourceRef = "LOCAL", Source = "N/A", SourceUrl = "N/A",
                        SourceDataset = "N/A", SourceVersion = "N/A", Licence = "N/A"
                    };
                    await _context.Provenance.AddAsync(local);
                    await _context.SaveChangesAsync();
                }
                provenanceId = local.Id;
            }
            else if (!await _context.Provenance.AnyAsync(x => x.Id == provenanceId))
            {
                throw new InvalidOperationException($"Provenance record {provenanceId} does not exist.");
            }

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
                    Callsign = callsign,
                    Embarkation = embarkation,
                    Destination = destination,
                    AirlineId = airlineId,
                    ProvenanceId = provenanceId
                };

                await _context.Flights.AddAsync(flight);
                await _context.SaveChangesAsync();
            }

            return flight;
        }
    }
}
