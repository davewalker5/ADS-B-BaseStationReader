using BaseStationReader.Entities.Import;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;

namespace BaseStationReader.BusinessLogic.Logging
{
    public class AircraftImporter : CsvImporter<AircraftMappingProfile, Aircraft>, IAircraftImporter
    {
        private readonly IAircraftManager _aircraftManager;
        private readonly IModelManager _modelManager;

        public AircraftImporter(
            IAircraftManager aircraftManager,
            IModelManager modelManager,
            ITrackerLogger logger) : base(logger)
        {
            _aircraftManager = aircraftManager;
            _modelManager = modelManager;
        }

        /// <summary>
        /// Read a set of Aircraft instances from a CSV file
        /// </summary>
        /// <param name="filePath"></param>
        /// <returns></returns>
        public override List<Aircraft> Read(string filePath)
        {
            // Load the data
            var aircraft = base.Read(filePath);
            if (aircraft?.Count > 0)
            {
                // Identify instances where there's no address and remove them
                aircraft.RemoveAll(x => string.IsNullOrEmpty(x.Address));
                Logger.LogMessage(Severity.Info, $"Aircraft with no address removed : {aircraft.Count} aircraft remaining");

                // Identify instances where there's no registration and remove them
                aircraft.RemoveAll(x => string.IsNullOrEmpty(x.Registration));
                Logger.LogMessage(Severity.Info, $"Aircraft with no registration removed : {aircraft.Count} aircraft remaining");

                // Identify instances where there's no model IATA or ICAO code and remove them
                aircraft.RemoveAll(x => string.IsNullOrEmpty(x.ModelICAO) && string.IsNullOrEmpty(x.ModelIATA));
                Logger.LogMessage(Severity.Info, $"Aircraft with no model IATA/ICAO code removed : {aircraft.Count} aircraft remaining");

                // Now identify the model for each one
                foreach (var a in aircraft)
                {
                    var model = Task.Run(() => _modelManager.GetAsync(a.ModelIATA, a.ModelICAO, a.Model?.Name)).Result;
                    a.ModelId = model?.Id ?? 0;
                }

                // Identify instances where the model doesn't exist and remove them
                aircraft.RemoveAll(x => x.ModelId == 0);
                Logger.LogMessage(Severity.Info, $"Aircraft with no model removed : {aircraft.Count} aircraft remaining");
            }

            return aircraft;
        }

        /// <summary>
        /// Save a collection of aircraft to the database
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        public override async Task SaveAsync(IEnumerable<Aircraft> aircraft)
        {
            if (aircraft?.Any() == true)
            {
                Logger.LogMessage(Severity.Info, $"Saving {aircraft.Count()} aircraft to the database");

                foreach (var a in aircraft)
                {
                    Logger.LogMessage(Severity.Debug, $"Saving Aircraft '{a.Address}' : " +
                        $"Registration = '{a.Registration}', " +
                        $"Model ICAO = '{a.ModelICAO}', " +
                        $"Model IATA = '{a.ModelIATA}', " +
                        $"Manufactured = {a.Manufactured}");

                    var age = a.Manufactured > 0 ? DateTime.Today.Year - a.Manufactured : null;
                    await _aircraftManager.AddAsync(a.Address, a.Registration, a.Manufactured, age, a.ModelId);
                }
            }
            else
            {
                Logger.LogMessage(Severity.Warning, $"No aircraft to save");
            }
        }
    }
}