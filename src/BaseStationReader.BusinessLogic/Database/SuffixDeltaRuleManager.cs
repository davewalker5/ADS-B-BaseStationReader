using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Heuristics;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class SuffixDeltaRuleManager : ISuffixDeltaRuleManager
    {
        private readonly BaseStationReaderDbContext _context;

        public SuffixDeltaRuleManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first suffix/delta rule matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<SuffixDeltaRule> GetAsync(Expression<Func<SuffixDeltaRule, bool>> predicate)
        {
            List<SuffixDeltaRule> rules = await ListAsync(predicate);
            return rules.FirstOrDefault();
        }

        /// <summary>
        /// Get a list of suffix/delta rules matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<SuffixDeltaRule>> ListAsync(Expression<Func<SuffixDeltaRule, bool>> predicate)
            => await _context.SuffixDeltaRules.Where(predicate).ToListAsync();

        /// <summary>
        /// Truncate the suffix/delta rules table to remove all existing entries
        /// </summary>
        /// <returns></returns>
        public async Task Truncate()
            => await _context.TruncateSuffixDeltaRules();
        
        /// <summary>
        /// Add a suffix delta rule
        /// </summary>
        /// <param name="airlineICAO"></param>
        /// <param name="airlineIATA"></param>
        /// <param name="suffix"></param>
        /// <param name="delta"></param>
        /// <param name="support"></param>
        /// <param name="purity"></param>
        /// <returns></returns>
        public async Task<SuffixDeltaRule> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string suffix,
            int delta,
            int support,
            decimal purity)
        {
            var rule = new SuffixDeltaRule()
            {
                AirlineICAO = airlineICAO,
                AirlineIATA = airlineIATA,
                Suffix = suffix,
                Delta = delta,
                Support = support,
                Purity = purity
            };

            await _context.SuffixDeltaRules.AddAsync(rule);
            await _context.SaveChangesAsync();
            return rule;
        }
    }
}