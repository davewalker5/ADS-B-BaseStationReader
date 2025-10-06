using BaseStationReader.Entities.Import;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.BusinessLogic.Logging
{
    public class NumberSuffixImporter : CsvImporter<NumberSuffixRuleMappingProfile, NumberSuffixRule>, INumberSuffixImporter
    {
        private readonly INumberSuffixRuleManager _numberSuffixManager;

        public NumberSuffixImporter(INumberSuffixRuleManager NumberSuffixManager, ITrackerLogger logger) : base(logger)
            => _numberSuffixManager = NumberSuffixManager;

        /// <summary>
        /// Read a set of numer suffix rules from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public override List<NumberSuffixRule> Read(string filePath)
        {
            var mappings = base.Read(filePath);
            return mappings;
        }

        /// <summary>
        /// Truncate the target table to remove existing entries
        /// </summary>
        /// <returns></returns>
        public async Task Truncate()
            => await _numberSuffixManager.Truncate();

        /// <summary>
        /// Save a collection of number suffix rules to the database
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public override async Task Save(IEnumerable<NumberSuffixRule> rules)
        {
            if (rules?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {rules.Count()} flight number/suffix rules to the database");

                foreach (var rule in rules)
                {
                    Logger.LogMessage(Severity.Debug, $"Saving number/suffix rule : " +
                        $"{rule.AirlineICAO}, {rule.AirlineIATA}, {rule.Numeric}, {rule.Digits}, {rule.Support}, {rule.Purity}");

                    await _numberSuffixManager.AddAsync(
                        rule.AirlineICAO,
                        rule.AirlineIATA,
                        rule.Numeric,
                        rule.Suffix,
                        rule.Digits,
                        rule.Support,
                        rule.Purity);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, $"No number/suffix rules to save");
            }
        }
    }
}