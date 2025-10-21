using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Api.Wrapper
{
    internal class AirlineApiWrapper : IAirlineApiWrapper
    {
        private readonly IExternalApiRegister _register;
        private readonly IDatabaseManagementFactory _factory;

        public AirlineApiWrapper(IExternalApiRegister register, IDatabaseManagementFactory factory)
        {
            _register = register;
            _factory = factory;
        }

        /// <summary>
        /// Look up an airline and save it locally
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Airline> LookupAirlineAsync(string icao, string iata, string name)
        {
            // At least one of the parameters must be specified
            if (string.IsNullOrEmpty(icao) && string.IsNullOrEmpty(iata) && string.IsNullOrEmpty(name))
            {
                LogMessage(Severity.Warning, icao, iata, name, "No valid details supplied for lookup");
                return null;
            }

            // See if the airline is stored locally, first
            LogMessage(Severity.Info, icao, iata, name, "Looking for airline in the database");
            var airline = await _factory.AirlineManager.GetAsync(iata, icao, name);
            if ((airline == null) &&
                !string.IsNullOrEmpty(iata) &&
                !string.IsNullOrEmpty(icao) &&
                !string.IsNullOrEmpty(name))
            {
                // The airline isn't stored locally but we have all the necessary properties to add it to the
                // database so do so
                LogMessage(Severity.Info, icao, iata, name, "Not stored locally, adding to the database");
                airline = await _factory.AirlineManager.AddAsync(iata, icao, name);
            }

            // Not in the database and we don't have complete properties, so use the API to look the airline up
            if (airline == null)
            {
                LogMessage(Severity.Info, icao, iata, name, "Not stored locally, using the API");

                // Get the API instance
                if (_register.GetInstance(ApiEndpointType.Airlines) is not IAirlinesApi api) return null;

                // Not stored locally, so use the API to look it up
                var properties = !string.IsNullOrEmpty(icao) ?
                    await api.LookupAirlineByICAOCodeAsync(icao) :
                    await api.LookupAirlineByIATACodeAsync(iata);

                if ((properties?.Count ?? 0) > 0)
                {
                    // Extract the airline properties from the response
                    properties.TryGetValue(ApiProperty.AirlineICAO, out string airlineICAO);
                    properties.TryGetValue(ApiProperty.AirlineIATA, out string airlineIATA);
                    properties.TryGetValue(ApiProperty.AirlineName, out string airlineName);

                    // Create a new airline object containing the details returned by the API
                    LogMessage(Severity.Info, airlineICAO, airlineIATA, airlineName, "Saving new airline to the database");
                    airline = await _factory.AirlineManager.AddAsync(airlineIATA, airlineICAO, airlineName);
                }
                else
                {
                    LogMessage(Severity.Info, icao, iata, name, "API lookup produced no results");
                }
            }
            else
            {
                LogMessage(Severity.Info, airline.ICAO, airline.IATA, airline.Name, "Retrieved from the database");
            }

            return airline;
        }

        /// <summary>
        /// Log a message concerning an airline lookup
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        private void LogMessage(Severity severity, string icao, string iata, string name, string message)
            => _factory.Logger.LogMessage(severity, $"Airline ICAO = {icao}, IATA = {iata}, Name = {name} : {message}");
    }
}