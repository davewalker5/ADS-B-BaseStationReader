using BaseStationReader.Data;
using BaseStationReader.Entities.Heuristics;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class ConfirmedMappingManager : IConfirmedMappingManager
    {
        private readonly BaseStationReaderDbContext _context;

        public ConfirmedMappingManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Truncate the confirmed mappings table to remove all existing entries
        /// </summary>
        /// <returns></returns>
        public async Task Truncate()
            => await _context.TruncateConfirmedMappings();
        
        /// <summary>
        /// Add a confirmed mapping between callsign and flight number
        /// </summary>
        /// <param name="airlineICAO"></param>
        /// <param name="airlineIATA"></param>
        /// <param name="flightIATA"></param>
        /// <param name="callsign"></param>
        /// <param name="digits"></param>
        /// <returns></returns>
        public async Task<ConfirmedMapping> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string flightIATA,
            string callsign,
            string digits)
        {
            var mapping = new ConfirmedMapping()
            {
                AirlineICAO = airlineICAO,
                AirlineIATA = airlineIATA,
                FlightIATA = flightIATA,
                Callsign = callsign,
                Digits = digits
            };

            await _context.ConfirmedMappings.AddAsync(mapping);
            await _context.SaveChangesAsync();
            return mapping;
        }
    }
}