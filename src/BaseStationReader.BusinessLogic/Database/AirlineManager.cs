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
            => await _context.Airlines.Include(x => x.Provenance).Where(predicate).ToListAsync();

        /// <summary>
        /// Add an airline, if it doesn't already exist
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Airline> AddAsync(string iata, string icao, string name, int provenanceId = 0)
        {
            // Clean the inputs so they're in a standardised format
            var cleanIATA = StringCleaner.CleanIATA(iata);
            var cleanICAO = StringCleaner.CleanICAO(icao);
            var cleanName = StringCleaner.CleanName(name);

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

            // Look for a matching record
            var airline = await GetAsync(cleanIATA, cleanICAO, cleanName);

            if (airline == null)
            {
                // No match, so create a new record
                airline = new Airline
                {
                    IATA = cleanIATA,
                    ICAO = cleanICAO,
                    Name = cleanName,
                    ProvenanceId = provenanceId
                };

                await _context.Airlines.AddAsync(airline);
                await _context.SaveChangesAsync();
            }

            return airline;
        }

        public async Task<Airline> UpdateAsync(int id, string iata, string icao, string name, int provenanceId)
        {
            var airline = await _context.Airlines.FindAsync(id)
                ?? throw new InvalidOperationException($"Airline record {id} does not exist.");
            if (!await _context.Provenance.AnyAsync(x => x.Id == provenanceId))
                throw new InvalidOperationException($"Provenance record {provenanceId} does not exist.");

            var cleanIATA = StringCleaner.CleanIATA(iata);
            var cleanICAO = StringCleaner.CleanICAO(icao);
            var cleanName = StringCleaner.CleanName(name);
            if (!string.IsNullOrEmpty(cleanIATA) &&
                await _context.Airlines.AnyAsync(x => x.Id != id && x.IATA == cleanIATA))
                throw new InvalidOperationException($"An airline with IATA code '{cleanIATA}' already exists.");
            if (!string.IsNullOrEmpty(cleanICAO) &&
                await _context.Airlines.AnyAsync(x => x.Id != id && x.ICAO == cleanICAO))
                throw new InvalidOperationException($"An airline with ICAO code '{cleanICAO}' already exists.");

            airline.IATA = cleanIATA;
            airline.ICAO = cleanICAO;
            airline.Name = cleanName;
            airline.ProvenanceId = provenanceId;
            await _context.SaveChangesAsync();
            return await GetAsync(x => x.Id == id);
        }

        public async Task DeleteAsync(int id)
        {
            var airline = await _context.Airlines.FindAsync(id)
                ?? throw new InvalidOperationException($"Airline record {id} does not exist.");
            _context.Airlines.Remove(airline);
            await _context.SaveChangesAsync();
        }
    }
}
