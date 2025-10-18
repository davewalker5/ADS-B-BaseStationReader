using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;

namespace BaseStationReader.BusinessLogic.Logging
{
    public class AirlineImporter : CsvImporter<AirlineMappingProfile, Airline>, IAirlineImporter
    {
        private readonly IDatabaseManagementFactory _factory;

        public AirlineImporter(IDatabaseManagementFactory factory) : base(factory.Logger)
            => _factory = factory;

        /// <summary>
        /// Read a set of airline instances from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public override List<Airline> Read(string filePath)
        {
            // Load the data
            var airlines = base.Read(filePath);
            if (airlines?.Count > 0)
            {
                // Remove any that aren't active
                airlines.RemoveAll(x => !x.Active);
                Logger.LogMessage(Severity.Info, $"Inactive airlines removed : {airlines.Count} airlines remaining");

                // Clean up the airline codes
                foreach (var airline in airlines.Where(x => Replacements.Contains(x.IATA)))
                {
                    airline.IATA = "";
                }

                foreach (var airline in airlines.Where(x => Replacements.Contains(x.ICAO)))
                {
                    airline.ICAO = "";
                }

                // Identify instances where there's no IATA or ICAO code and remove them
                airlines.RemoveAll(x => string.IsNullOrEmpty(x.ICAO) && string.IsNullOrEmpty(x.IATA));
                Logger.LogMessage(Severity.Info, $"Airlines with no IATA/ICAO code removed : {airlines.Count} airlines remaining");
            }

            return airlines;
        }

        /// <summary>
        /// Save a collection of airlines to the database
        /// </summary>
        /// <param name="airlines"></param>
        /// <returns></returns>
        public override async Task SaveAsync(IEnumerable<Airline> airlines)
        {
            if (airlines?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {airlines.Count()} airlines to the database");

                foreach (var airline in airlines)
                {
                    Logger.LogMessage(Severity.Debug, $"Saving airline '{airline.Name}' : IATA = '{airline.IATA}', ICAO = '{airline.ICAO}'");
                    await _factory.AirlineManager.AddAsync(airline.IATA, airline.ICAO, airline.Name);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, $"No airlines to save");
            }
        }
    }
}