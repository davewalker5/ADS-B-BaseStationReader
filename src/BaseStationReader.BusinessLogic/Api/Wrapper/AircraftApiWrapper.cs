using System.Globalization;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    internal class AircraftApiWrapper : IAircraftApiWrapper
    {
        private readonly ITrackerLogger _logger;
        private readonly IExternalApiRegister _register;
        private readonly IAircraftManager _aircraftManager;
        private readonly IManufacturerManager _manufacturerManager;
        private readonly IModelManager _modelManager;

        public AircraftApiWrapper(
            ITrackerLogger logger,
            IExternalApiRegister register,
            IAircraftManager aircraftManager,
            IModelManager modelManager,
            IManufacturerManager manufacturerManager)
        {
            _logger = logger;
            _register = register;
            _aircraftManager = aircraftManager;
            _modelManager = modelManager;
            _manufacturerManager = manufacturerManager;
        }

        /// <summary>
        /// Look up an aircraft and save it locally
        /// </summary>
        /// <param name="address"></param>
        /// <param name="alternateModelICAO"></param>
        /// <returns></returns>
        public async Task<Aircraft> LookupAircraftAsync(string address, string alternateModelICAO)
        {
            // The aircraft address must be specified
            if (string.IsNullOrEmpty(address))
            {
                _logger.LogMessage(Severity.Warning, $"Unable to look up aircraft details : Invalid aircraft address");
                return null;
            }

            // See if the aircraft is stored locally, first
            var aircraft = await _aircraftManager.GetAsync(x => x.Address == address);
            if (aircraft == null)
            {
                // Get the API instance
                if (_register.GetInstance(ApiEndpointType.Aircraft) is not IAircraftApi api) return null;

                _logger.LogMessage(Severity.Info, $"Aircraft {address} is not stored locally : Using the API");

                // Not stored locally, so use the API to look it up
                var properties = await api.LookupAircraftAsync(address);
                if ((properties?.Count ?? 0) > 0)
                {
                    // If the aircraft is returned without a model and we have and alternative ICAO for the
                    // model (often from the flight), then use that
                    var modelICAO = string.IsNullOrEmpty(properties[ApiProperty.ModelICAO]) ?
                        alternateModelICAO ?? "" :
                        properties[ApiProperty.ModelICAO];

                    // Get the year of manufacture of the aircraft and determine its age
                    var manufactured = GetYearOfManufacture(properties[ApiProperty.AircraftManufactured]);
                    int? age = manufactured != null ? DateTime.Today.Year - manufactured : null;

                    // Save the manufacturer, model and aircraft
                    var manufacturer = await _manufacturerManager.AddAsync(properties[ApiProperty.ManufacturerName]);
                    var model = await _modelManager.AddAsync(
                        properties[ApiProperty.ModelIATA], modelICAO, properties[ApiProperty.ModelName], manufacturer.Id);
                    aircraft = await _aircraftManager.AddAsync(
                        address, properties[ApiProperty.AircraftRegistration], manufactured, age, model.Id);

                    // TODO: Remove this
                    // If the properties contain an active flight callsign, pass it back to the caller
                    // if (properties.TryGetValue(ApiProperty.Callsign, out string callsign))
                    // {
                    //     aircraft.Callsign = callsign;
                    // } 
                }
                else
                {
                    _logger.LogMessage(Severity.Info, $"API lookup for aircraft {address} produced no results");
                }
            }
            else
            {
                _logger.LogMessage(Severity.Info, $"Aircraft {address} retrieved from the database");
            }

            return aircraft;
        }

        /// <summary>
        /// Extract the year of manufacture from a string representation of either the integer year or
        /// a date
        /// </summary>
        /// <param name="manufactured"></param>
        /// <returns></returns>
        private static int? GetYearOfManufacture(string manufactured)
        {
            if (!string.IsNullOrEmpty(manufactured))
            {
                if (int.TryParse(manufactured, out int year))
                {
                    return year;
                }

                if (DateTime.TryParseExact(manufactured, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateOfManufacture))
                {
                    return dateOfManufacture.Year;
                }
            }

            return null;
        }
    }
}