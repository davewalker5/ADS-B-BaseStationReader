using BaseStationReader.Data;
using BaseStationReader.Entities.Heuristics;
using BaseStationReader.Interfaces.Database;

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
        /// Truncate the confirmed mappings table to remove all existing entries
        /// </summary>
        /// <returns></returns>
        public async Task Truncate()
            => await _context.TruncateNumberSuffixes();
        
        /// <summary>
        /// Add a number/suffix rule
        /// </summary>
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