using BaseStationReader.Entities.Import;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Entities.Messages;
using BaseStationReader.Interfaces.DataExchange;

namespace BaseStationReader.BusinessLogic.Logging
{
    public class NumberSuffixImporter : CsvImporter<NumberSuffixMappingProfile, NumberSuffix>, INumberSuffixImporter
    {
        private readonly INumberSuffixManager _numberSuffixManager;

        public NumberSuffixImporter(INumberSuffixManager NumberSuffixManager, ITrackerLogger logger) : base(logger)
            => _numberSuffixManager = NumberSuffixManager;

        /// <summary>
        /// Read a set of numer suffix rules from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public override List<NumberSuffix> Read(string filePath)
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
        public override async Task Save(IEnumerable<NumberSuffix> rules)
        {
            if (rules?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {rules.Count()} flight number mappings to the database");

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