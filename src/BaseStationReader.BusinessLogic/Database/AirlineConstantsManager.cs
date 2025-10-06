using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Heuristics;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class AirlineConstantsManager : IAirlineConstantsManager
    {
        private readonly BaseStationReaderDbContext _context;

        public AirlineConstantsManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first set of airline constants matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<AirlineConstants> GetAsync(Expression<Func<AirlineConstants, bool>> predicate)
        {
            List<AirlineConstants> constants = await ListAsync(predicate);
            return constants.FirstOrDefault();
        }

        /// <summary>
        /// Get a list of airline constants matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<AirlineConstants>> ListAsync(Expression<Func<AirlineConstants, bool>> predicate)
            => await _context.AirlineConstants.Where(predicate).ToListAsync();

        /// <summary>
        /// Truncate the airline constants table to remove all existing entries
        /// </summary>
        /// <returns></returns>
        public async Task Truncate()
            => await _context.TruncateAirlineConstants();
        
        /// <summary>
        /// Add a set of airline constants
        /// </summary>
        /// <param name="airlineICAO"></param>
        /// <param name="airlineIATA"></param>
        /// <param name="delta"></param>
        /// <param name="purity"></param>
        /// <param name="prefix"></param>
        /// <param name="identityRate"></param>
        /// <returns></returns>
        public async Task<AirlineConstants> AddAsync(
            string airlineICAO,
            string airlineIATA,
            int? delta,
            decimal purity,
            string prefix,
            decimal identityRate)
        {
            var rule = new AirlineConstants()
            {
                AirlineICAO = airlineICAO,
                AirlineIATA = airlineIATA,
                ConstantDelta = delta,
                ConstantDeltaPurity = purity,
                ConstantPrefix = prefix,
                IdentityRate = identityRate
            };

            await _context.AirlineConstants.AddAsync(rule);
            await _context.SaveChangesAsync();
            return rule;
        }
    }
}