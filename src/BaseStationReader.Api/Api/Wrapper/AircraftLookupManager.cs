using System.Globalization;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Api.Wrapper
{
    internal class AircraftLookupManager : IAircraftLookupManager
    {
        private readonly IExternalApiRegister _register;
        private readonly IDatabaseManagementFactory _factory;

        public AircraftLookupManager(IExternalApiRegister register, IDatabaseManagementFactory factory)
        {
            _register = register;
            _factory = factory;
        }

        /// <summary>
        /// Identify an aircraft given its 24-bit ICAO address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Aircraft> IdentifyAircraftAsync(string address)
        {
            // Attempt to load an aircraft from the database. If it's not stored locally, use the API to look it up
            var aircraft = await LoadAircraftAsync(address);
            aircraft ??= await LookupAircraftAsync(address);

            // Log the aircraft details
            if (aircraft != null)
            {
                LogAircraftDetails(aircraft);
            }

            return aircraft;
        }

        /// <summary>
        /// Attempt to load an aircraft from the database
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private async Task<Aircraft> LoadAircraftAsync(string address)
        {
            LogMessage(Severity.Info, address, $"Attempting to retrieve the aircraft from the database");
            var aircraft = await _factory.AircraftManager.GetAsync(x => x.Address == address);
            if (aircraft == null)
            {
                LogMessage(Severity.Info, address, $"Aircraft is not stored locally");
            }

            return aircraft;
        }

        /// <summary>
        /// Lookup the aircraft
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private async Task<Aircraft> LookupAircraftAsync(string address)
        {
            Aircraft aircraft = null;

            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.Aircraft) is not IAircraftApi api)
            {
                LogMessage(Severity.Error, address, $"Registered aircraft API is not an instance of {typeof(IAircraftApi).Name}");
                return null;
            }

            LogMessage(Severity.Info, address, $"Using the {api.GetType().Name} API to look up aircraft details");

            // Not stored locally, so use the API to look it up
            var properties = await api.LookupAircraftAsync(address);
            if ((properties?.Count ?? 0) > 0)
            {
                // Get the year of manufacture of the aircraft and determine its age
                var manufactured = GetYearOfManufacture(properties[ApiProperty.AircraftManufactured]);
                int? age = manufactured != null ? DateTime.Today.Year - manufactured : null;

                // Save the manufacturer, model and aircraft
                var manufacturer = await _factory.ManufacturerManager.AddAsync(properties[ApiProperty.ManufacturerName]);
                var model = await _factory.ModelManager.AddAsync(
                    properties[ApiProperty.ModelIATA], properties[ApiProperty.ModelICAO], properties[ApiProperty.ModelName], manufacturer.Id);
                aircraft = await _factory.AircraftManager.AddAsync(
                    address, properties[ApiProperty.AircraftRegistration], manufactured, age, model.Id);
            }
            else
            {
                LogMessage(Severity.Info, address, "API lookup produced no results");
            }

            return aircraft;
        }

        /// <summary>
        /// Output a message formatted with the aircraft address
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="address"></param>
        /// <param name="message"></param>
        private void LogMessage(Severity severity, string address, string message)
            => _factory.Logger.LogMessage(severity, $"Aircraft '{address}': {message}");

        /// <summary>
        /// Log the details for an aircraft
        /// </summary>
        /// <param name="aircraft"></param>
        private void LogAircraftDetails(Aircraft aircraft)
            => LogMessage(Severity.Info, aircraft.Address, 
                $"Identified aircraft: " +
                $"Registration = {aircraft.Registration}, " +
                $"Model = {aircraft.Model.IATA}, {aircraft.Model.ICAO}, {aircraft.Model.Name}, " +
                $"Manufacturer = {aircraft.Model.Manufacturer.Name}, " +
                $"Manufactured = {aircraft.Manufactured}");

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