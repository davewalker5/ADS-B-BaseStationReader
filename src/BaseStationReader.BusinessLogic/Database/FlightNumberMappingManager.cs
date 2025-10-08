using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class FlightNumberMappingManager : IFlightNumberMappingManager
    {
        private readonly BaseStationReaderDbContext _context;

        public FlightNumberMappingManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first confirmed mapping matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<FlightNumberMapping> GetAsync(Expression<Func<FlightNumberMapping, bool>> predicate)
        {
            List<FlightNumberMapping> mappings = await ListAsync(predicate);
            return mappings.FirstOrDefault();
        }

        /// <summary>
        /// Get a list of confirmed mappings matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<FlightNumberMapping>> ListAsync(Expression<Func<FlightNumberMapping, bool>> predicate)
            => await _context.FlightNumberMappings.Where(predicate).ToListAsync();
        
        /// <summary>
        /// Add a confirmed mapping between callsign and flight number
        /// </summary>
        /// <param name="airlineICAO"></param>
        /// <param name="airlineIATA"></param>
        /// <param name="airlineName"></param>
        /// <param name="airportICAO"></param>
        /// <param name="airportIATA"></param>
        /// <param name="airportName"></param>
        /// <param name="airportType"></param>
        /// <param name="flightIATA"></param>
        /// <param name="callsign"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task<FlightNumberMapping> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string airlineName,
            string airportICAO,
            string airportIATA,
            string airportName,
            AirportType airportType,
            string flightIATA,
            string callsign,
            string filename)
        {
            // See if the mapping already exists, based on the callsign
            var mapping = await _context.FlightNumberMappings.FirstOrDefaultAsync(x => x.Callsign == callsign);
            if (mapping != null)
            {
                // Already exists, so just update its properties
                mapping.AirlineICAO = airlineICAO;
                mapping.AirlineIATA = airlineIATA;
                mapping.AirlineName = airlineName;
                mapping.AirportICAO = airportICAO;
                mapping.AirportIATA = airportIATA;
                mapping.AirportName = airportName;
                mapping.AirportType = airportType;
                mapping.FlightIATA = flightIATA;
                mapping.FileName = filename;
            }
            else
            {
                // Doesn't exist, so create a new mapping
                mapping = new FlightNumberMapping()
                {
                    AirlineICAO = airlineICAO,
                    AirlineIATA = airlineIATA,
                    AirlineName = airlineName,
                    AirportICAO = airportICAO,
                    AirportIATA = airportIATA,
                    AirportName = airportName,
                    AirportType = airportType,
                    FlightIATA = flightIATA,
                    Callsign = callsign,
                    FileName = filename
                };

                // Add it to the database
                await _context.FlightNumberMappings.AddAsync(mapping);
            }

            await _context.SaveChangesAsync();
            return mapping;
        }
    }
}