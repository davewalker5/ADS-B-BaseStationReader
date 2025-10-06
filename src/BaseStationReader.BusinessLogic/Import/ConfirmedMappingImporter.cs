using BaseStationReader.Entities.Import;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Entities.Heuristics;

namespace BaseStationReader.BusinessLogic.Logging
{
    public class ConfirmedMappingImporter : CsvImporter<ConfirmedMappingProfile, ConfirmedMapping>, IConfirmedMappingImporter
    {
        private readonly IConfirmedMappingManager _confirmedMappingManager;

        public ConfirmedMappingImporter(IConfirmedMappingManager confirmedMappingManager, ITrackerLogger logger) : base(logger)
            => _confirmedMappingManager = confirmedMappingManager;

        /// <summary>
        /// Read a set of confirmed mapping instances from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public override List<ConfirmedMapping> Read(string filePath)
        {
            var mappings = base.Read(filePath);
            return mappings;
        }

        /// <summary>
        /// Truncate the target table to remove existing entries
        /// </summary>
        /// <returns></returns>
        public async Task Truncate()
            => await _confirmedMappingManager.Truncate();

        /// <summary>
        /// Save a collection of confirmed mappings to the database
        /// </summary>
        /// <param name="mappings"></param>
        /// <param name="truncate"></param>
        /// <returns></returns>
        public override async Task Save(IEnumerable<ConfirmedMapping> mappings)
        {
            if (mappings?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {mappings.Count()} flight number mappings to the database");

                foreach (var mapping in mappings)
                {
                    Logger.LogMessage(Severity.Debug, $"Saving flight number mapping : " +
                    $"{mapping.AirlineICAO}, {mapping.AirlineIATA}, {mapping.FlightIATA}, {mapping.Callsign}, {mapping.Digits}");

                    await _confirmedMappingManager.AddAsync(
                        mapping.AirlineICAO,
                        mapping.AirlineIATA,
                        mapping.FlightIATA,
                        mapping.Callsign,
                        mapping.Digits);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, $"No flight number mappings to save");
            }
        }
    }
}