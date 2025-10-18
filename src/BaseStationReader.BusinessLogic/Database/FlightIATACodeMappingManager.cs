using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class FlightIATACodeMappingManager : IFlightIATACodeMappingManager
    {
        private readonly BaseStationReaderDbContext _context;

        public FlightIATACodeMappingManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first confirmed mapping matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<FlightIATACodeMapping> GetAsync(Expression<Func<FlightIATACodeMapping, bool>> predicate)
        {
            List<FlightIATACodeMapping> mappings = await ListAsync(predicate);
            return mappings.FirstOrDefault();
        }

        /// <summary>
        /// Get a list of confirmed mappings matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<FlightIATACodeMapping>> ListAsync(Expression<Func<FlightIATACodeMapping, bool>> predicate)
            => await _context.FlightIATACodeMappings.Where(predicate).ToListAsync();
        
        /// <summary>
        /// Add a confirmed mapping between callsign and flight IATA code
        /// </summary>
        /// <param name="airlineICAO"></param>
        /// <param name="airlineIATA"></param>
        /// <param name="airlineName"></param>
        /// <param name="airportICAO"></param>
        /// <param name="airportIATA"></param>
        /// <param name="airportName"></param>
        /// <param name="airportType"></param>
        /// <param name="embarkation"></param>
        /// <param name="destination"></param>
        /// <param name="flightIATA"></param>
        /// <param name="callsign"></param>
        /// <param name="filename"></param>
        /// <returns></returns>
        public async Task<FlightIATACodeMapping> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string airlineName,
            string airportICAO,
            string airportIATA,
            string airportName,
            AirportType airportType,
            string embarkation,
            string destination,
            string flightIATA,
            string callsign,
            string filename)
        {
            // See if the mapping already exists, based on the callsign
            var mapping = await _context.FlightIATACodeMappings.FirstOrDefaultAsync(x => x.Callsign == callsign);
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
                mapping.Embarkation = embarkation;
                mapping.Destination = destination;
                mapping.FlightIATA = flightIATA;
                mapping.FileName = filename;
            }
            else
            {
                // Doesn't exist, so create a new mapping
                mapping = new FlightIATACodeMapping()
                {
                    AirlineICAO = airlineICAO,
                    AirlineIATA = airlineIATA,
                    AirlineName = airlineName,
                    AirportICAO = airportICAO,
                    AirportIATA = airportIATA,
                    AirportName = airportName,
                    AirportType = airportType,
                    Embarkation = embarkation,
                    Destination = destination,
                    FlightIATA = flightIATA,
                    Callsign = callsign,
                    FileName = filename
                };

                // Add it to the database
                await _context.FlightIATACodeMappings.AddAsync(mapping);
            }

            await _context.SaveChangesAsync();
            return mapping;
        }
    }
}