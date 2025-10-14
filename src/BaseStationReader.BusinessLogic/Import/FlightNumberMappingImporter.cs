using BaseStationReader.Entities.Import;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;
using BaseStationReader.Entities.Api;

namespace BaseStationReader.BusinessLogic.Logging
{
    public class FlightNumberMappingImporter : CsvImporter<FlightNumberMappingProfile, FlightNumberMapping>, IFlightNumberMappingImporter
    {
        private readonly IFlightNumberMappingManager _confirmedMappingManager;

        public FlightNumberMappingImporter(IFlightNumberMappingManager confirmedMappingManager, ITrackerLogger logger) : base(logger)
            => _confirmedMappingManager = confirmedMappingManager;

        /// <summary>
        /// Read a set of confirmed mapping instances from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public override List<FlightNumberMapping> Read(string filePath)
        {
            var mappings = base.Read(filePath);
            return mappings;
        }

        /// <summary>
        /// Save a collection of confirmed mappings to the database
        /// </summary>
        /// <param name="mappings"></param>
        /// <param name="truncate"></param>
        /// <returns></returns>
        public override async Task SaveAsync(IEnumerable<FlightNumberMapping> mappings)
        {
            if (mappings?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {mappings.Count()} flight number mappings to the database");

                foreach (var mapping in mappings)
                {
                    Logger.LogMessage(Severity.Debug, $"Saving flight number mapping : " +
                    $"{mapping.AirlineICAO}, {mapping.AirlineIATA}, {mapping.AirlineName}, " +
                    $"{mapping.AirportICAO}, {mapping.AirportIATA}, {mapping.AirportName}, {mapping.AirportType}, " +
                    $"{mapping.Embarkation}, {mapping.Destination}, {mapping.FlightIATA}, " +
                    $"{mapping.Callsign}, {mapping.FileName}");

                    await _confirmedMappingManager.AddAsync(
                        mapping.AirlineICAO,
                        mapping.AirlineIATA,
                        mapping.AirlineName,
                        mapping.AirportICAO,
                        mapping.AirportIATA,
                        mapping.AirportName,
                        mapping.AirportType,
                        mapping.Embarkation,
                        mapping.Destination,
                        mapping.FlightIATA,
                        mapping.Callsign,
                        mapping.FileName);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, $"No flight number mappings to save");
            }
        }
    }
}