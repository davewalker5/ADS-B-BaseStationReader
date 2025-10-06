using BaseStationReader.Entities.Import;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.BusinessLogic.Logging
{
    public class AirlineConstantsImporter : CsvImporter<AirlineConstantsMappingProfile, AirlineConstants>, IAirlineConstantsImporter
    {
        private readonly IAirlineConstantsManager _numberSuffixManager;

        public AirlineConstantsImporter(IAirlineConstantsManager NumberSuffixManager, ITrackerLogger logger) : base(logger)
            => _numberSuffixManager = NumberSuffixManager;

        /// <summary>
        /// Read a set of airline constants from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public override List<AirlineConstants> Read(string filePath)
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
        /// Save a collection of airline constants to the database
        /// </summary>
        /// <param name="rules"></param>
        /// <returns></returns>
        public override async Task Save(IEnumerable<AirlineConstants> rules)
        {
            if (rules?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {rules.Count()} airline constants to the database");

                foreach (var rule in rules)
                {
                    Logger.LogMessage(Severity.Debug, $"Saving number/suffix rule : " +
                        $"{rule.AirlineICAO}, {rule.AirlineIATA}, {rule.ConstantDelta}, {rule.ConstantDeltaPurity}, {rule.ConstantPrefix}, {rule.IdentityRate}");

                    await _numberSuffixManager.AddAsync(
                        rule.AirlineICAO,
                        rule.AirlineIATA,
                        rule.ConstantDelta,
                        rule.ConstantDeltaPurity,
                        rule.ConstantPrefix,
                        rule.IdentityRate);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, $"No airline constants to save");
            }
        }
    }
}