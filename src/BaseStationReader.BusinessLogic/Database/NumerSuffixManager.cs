using BaseStationReader.Data;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.BusinessLogic.Database
{
    internal class NumberSuffixManager : INumberSuffixManager
    {
        private readonly BaseStationReaderDbContext _context;

        public NumberSuffixManager(BaseStationReaderDbContext context)
        {
            _context = context;
        }
        
        /// <summary>
        /// Add a number/suffix rule
        /// </summary>
        /// <returns></returns>
        public async Task<NumberSuffix> AddAsync(
            string airlineICAO,
            string airlineIATA,
            string numeric,
            string suffix,
            string digits,
            int support,
            decimal purity)
        {
            var rule = new NumberSuffix()
            {
                AirlineICAO = airlineICAO,
                AirlineIATA = airlineIATA,
                Numeric = numeric,
                Suffix = suffix,
                Digits = digits,
                Support = support,
                Purity = purity
            };

            await _context.NumberSuffixes.AddAsync(rule);
            await _context.SaveChangesAsync();
            return rule;
        }
    }
}