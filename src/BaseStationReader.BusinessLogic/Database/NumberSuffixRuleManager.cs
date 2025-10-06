using System.Linq.Expressions;
using BaseStationReader.Data;
using BaseStationReader.Entities.Heuristics;
using BaseStationReader.Interfaces.Database;
using Microsoft.EntityFrameworkCore;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class NumberSuffixRuleManager : INumberSuffixRuleManager
    {
        private readonly BaseStationReaderDbContext _context;

        public NumberSuffixRuleManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Return the first number/suffix rule matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<NumberSuffixRule> GetAsync(Expression<Func<NumberSuffixRule, bool>> predicate)
        {
            List<NumberSuffixRule> rules = await ListAsync(predicate);
            return rules.FirstOrDefault();
        }

        /// <summary>
        /// Get a list of number/suffix rules matching the specified criteria
        /// </summary>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public async Task<List<NumberSuffixRule>> ListAsync(Expression<Func<NumberSuffixRule, bool>> predicate)
            => await _context.NumberSuffixRules.Where(predicate).ToListAsync();

        /// <summary>
        /// Truncate the number/suffix rules table to remove all existing entries
        /// </summary>
        /// <returns></returns>
        public async Task Truncate()
            => await _context.TruncateNumberSuffixRules();
        
        /// <summary>
        /// Add a number/suffix rule
        /// </summary>
        /// <param name="airlineICAO"></param>
        /// <param name="airlineIATA"></param>
        /// <param name="numeric"></param>
        /// <param name="suffix"></param>
        /// <param name="digits"></param>
        /// <param name="support"></param>
        /// <param name="purity"></param>
        /// <returns></returns>
        public async Task<NumberSuffixRule> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string numeric,
            string suffix,
            string digits,
            int support,
            decimal purity)
        {
            var rule = new NumberSuffixRule()
            {
                AirlineICAO = airlineICAO,
                AirlineIATA = airlineIATA,
                Numeric = numeric,
                Suffix = suffix,
                Digits = digits,
                Support = support,
                Purity = purity
            };

            await _context.NumberSuffixRules.AddAsync(rule);
            await _context.SaveChangesAsync();
            return rule;
        }
    }
}