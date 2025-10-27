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
        /// Log the details for an aircraft
        /// </summary>
        /// <param name="aircraft"></param>
        private void LogAircraftDetails(Aircraft aircraft)
            => _factory.Logger.LogMessage(Severity.Info,
                $"Identified aircraft: " +
                $"Address = {aircraft.Address}, " +
                $"Registration = {aircraft.Registration}, " +
                $"Model = {aircraft.Model.IATA}, {aircraft.Model.ICAO}, {aircraft.Model.Name}, " +
                $"Manufacturer = {aircraft.Model.Manufacturer.Name}, " +
                $"Manufactured = {aircraft.Manufactured}");

        /// <summary>
        /// Attempt to load an aircraft from the database
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        private async Task<Aircraft> LoadAircraftAsync(string address)
        {
            _factory.Logger.LogMessage(Severity.Info, $"Looking up aircraft {address} in the database");
            var aircraft = await _factory.AircraftManager.GetAsync(x => x.Address == address);
            if (aircraft == null)
            {
                _factory.Logger.LogMessage(Severity.Info, $"Aircraft '{address}' is not stored locally");
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
                _factory.Logger.LogMessage(Severity.Error, $"Registered aircraft API is not an instance of {typeof(IAircraftApi).Name}");
                return null;
            }

            _factory.Logger.LogMessage(Severity.Info, $"Using the API to look up details for aircraft '{address}'");

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
                _factory.Logger.LogMessage(Severity.Info, $"API lookup for aircraft {address} produced no results");
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