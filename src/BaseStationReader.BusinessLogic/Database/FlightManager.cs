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
                .Include(x => x.OriginAirport)
                .Include(x => x.DestinationAirport)
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
            int airlineId,
            int originAirportId,
            int destinationAirportId,
            int provenanceId = 0,
            string embarkation = "",
            string destination = "")
        {
            if (string.IsNullOrWhiteSpace(callsign))
                throw new ArgumentException("A callsign is required.", nameof(callsign));

            callsign = callsign.Trim().ToUpperInvariant();

            if (provenanceId == 0)
            {
                var local = await _context.Provenance.FirstOrDefaultAsync(x => x.SourceRef == "MANUAL");
                if (local == null)
                {
                    local = new Provenance
                    {
                        SourceRef = "MANUAL", Source = "N/A", SourceUrl = "N/A",
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

            if (!await _context.Airlines.AnyAsync(x => x.Id == airlineId))
                throw new InvalidOperationException($"Airline record {airlineId} does not exist.");
            if (!await _context.Airports.AnyAsync(x => x.Id == originAirportId))
                throw new InvalidOperationException($"Origin airport record {originAirportId} does not exist.");
            if (!await _context.Airports.AnyAsync(x => x.Id == destinationAirportId))
                throw new InvalidOperationException($"Destination airport record {destinationAirportId} does not exist.");

            // Callsign is the stable local identifier used to resolve tracked flights.
            var flight = await GetAsync(x => x.Callsign == callsign);

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
                    OriginAirportId = originAirportId,
                    DestinationAirportId = destinationAirportId,
                    ProvenanceId = provenanceId
                };

                await _context.Flights.AddAsync(flight);
                await _context.SaveChangesAsync();
            }

            return flight;
        }
    }
}
