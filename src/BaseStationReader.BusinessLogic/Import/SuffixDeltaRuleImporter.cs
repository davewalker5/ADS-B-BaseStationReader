using BaseStationReader.Entities.Import;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.BusinessLogic.Logging
{
    public class SuffixDeltaRuleImporter : CsvImporter<SuffixDeltaRuleMappingProfile, SuffixDeltaRule>, ISuffixDeltaRuleImporter
    {
        private readonly ISuffixDeltaRuleManager _suffixDeltaRuleManager;

        public SuffixDeltaRuleImporter(ISuffixDeltaRuleManager suffixDeltaRuleManager, ITrackerLogger logger) : base(logger)
            => _suffixDeltaRuleManager = suffixDeltaRuleManager;

        /// <summary>
        /// Read a set of numer suffix rules from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public override List<SuffixDeltaRule> Read(string filePath)
        {
            var mappings = base.Read(filePath);
            return mappings;
        }

        /// <summary>
        /// Truncate the target table to remove existing entries
        /// </summary>
        /// <returns></returns>
        public async Task Truncate()
            => await _suffixDeltaRuleManager.Truncate();

        /// <summary>
        /// Save a collection of number suffix rules to the database
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public override async Task Save(IEnumerable<SuffixDeltaRule> rules)
        {
            if (rules?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {rules.Count()} flight number/suffix rules to the database");

                foreach (var rule in rules)
                {
                    Logger.LogMessage(Severity.Debug, $"Saving flight number suffix delta rule : " +
                        $"{rule.AirlineICAO}, {rule.AirlineIATA}, {rule.Delta}, {rule.Support}, {rule.Purity}");

                    await _suffixDeltaRuleManager.AddAsync(
                        rule.AirlineICAO,
                        rule.AirlineIATA,
                        rule.Suffix,
                        rule.Delta,
                        rule.Support,
                        rule.Purity);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, $"No flight number suffix delta rules to save");
            }
        }
    }
}