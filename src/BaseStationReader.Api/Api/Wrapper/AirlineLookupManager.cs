using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Api.Wrapper
{
    internal class AirlineLookupManager : IAirlineLookupManager
    {
        private readonly IExternalApiRegister _register;
        private readonly IDatabaseManagementFactory _factory;

        public AirlineLookupManager(IExternalApiRegister register, IDatabaseManagementFactory factory)
        {
            _register = register;
            _factory = factory;
        }

        /// <summary>
        /// Identify an airline given its IATA code, ICAO code and/or name
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        public async Task<Airline> IdentifyAirlineAsync(string iata, string icao, string name)
        {
            // At least one of the parameters must be specified
            if (string.IsNullOrEmpty(icao) && string.IsNullOrEmpty(iata) && string.IsNullOrEmpty(name))
            {
                _factory.Logger.LogMessage(Severity.Warning, "No valid details supplied for airline lookup");
                return null;
            }

            // Attempt to load the airline from the database. If it's not there, see if the details are sufficient
            // to store it. If not, use the API to look it up
            var airline = await LoadAirlineAsync(iata, icao, name);
            airline ??= await SaveAirlineAsync(iata, icao, name);
            airline ??= await LookupAirlineAsync(iata, icao);

            // Log the airline details
            if (airline != null)
            {
                LogAirlineDetails(airline);
            }

            return airline;
        }

        /// <summary>
        /// Log the details for an airline
        /// </summary>
        /// <param name="airline"></param>
        private void LogAirlineDetails(Airline airline)
            => LogMessage(Severity.Info, airline.IATA, airline.ICAO, $"Identified airline '{airline.Name}'");

        /// <summary>
        /// Attempt to load an airline from the database
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<Airline> LoadAirlineAsync(string iata, string icao, string name)
        {
            LogMessage(Severity.Info, iata, icao, name, $"Attempting to retrieve airline from the database");
            var airline = await _factory.AirlineManager.GetAsync(iata, icao, name);
            if (airline == null)
            {
                LogMessage(Severity.Info, iata, icao, name, $"Airline is not stored locally");
            }

            return airline;
        }

        /// <summary>
        /// Attempt to load an airline from the database
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<Airline> SaveAirlineAsync(string iata, string icao, string name)
        {
            Airline airline = null;

            LogMessage(Severity.Info, iata, icao, name, $"Attempting to save airline to the database");
            if (!string.IsNullOrEmpty(iata) && !string.IsNullOrEmpty(icao) && !string.IsNullOrEmpty(name))
            {
                airline = await _factory.AirlineManager.AddAsync(iata, icao, name);
            }
            else
            {
                LogMessage(Severity.Info, iata, icao, name, $"Insufficient details to save airline");
            }

            return airline;
        }

        /// <summary>
        /// Lookup the airline
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <returns></returns>
        private async Task<Airline> LookupAirlineAsync(string iata, string icao)
        {
            Airline airline = null;

            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.Airlines) is not IAirlinesApi api)
            {
                LogMessage(Severity.Error, iata, icao, $"Registered airlines API is not an instance of {typeof(IAirlinesApi).Name}");
                return null;
            }

            LogMessage(Severity.Info, iata, icao, $"Using the {api.GetType().Name} API to look up airline details");

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
                LogMessage(Severity.Info, airlineIATA, airlineICAO, airlineName, "Saving new airline to the database");
                airline = await _factory.AirlineManager.AddAsync(airlineIATA, airlineICAO, airlineName);
            }
            else
            {
                LogMessage(Severity.Info, icao, iata, "API lookup produced no results");
            }

            return airline;
        }

        /// <summary>
        /// Output a message formatted with the airline details
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        private void LogMessage(Severity severity, string iata, string icao, string name, string message)
            => _factory.Logger.LogMessage(severity, $"Airline IATA '{iata}', ICAO '{icao}', Name '{name}': {message}");

        /// <summary>
        /// Output a message formatted with the airline details
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <param name="name"></param>
        /// <param name="message"></param>
        private void LogMessage(Severity severity, string iata, string icao, string message)
            => _factory.Logger.LogMessage(severity, $"Airline IATA '{iata}', ICAO '{icao}': {message}");
    }
}