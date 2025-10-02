using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    internal class AirlineApiWrapper : IAirlineApiWrapper
    {
        private readonly ITrackerLogger _logger;
        private readonly IExternalApiRegister _register;
        private readonly IAirlineManager _airlineManager;

        public AirlineApiWrapper(
            ITrackerLogger logger,
            IExternalApiRegister register,
            IAirlineManager airlineManager)
        {
            _logger = logger;
            _register = register;
            _airlineManager = airlineManager;
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

            // See if the airline is stored locally, first. Search by IATA and ICAO code and if that doesn't
            // produce a result search by name, assuming one has been provided
            LogMessage(Severity.Info, icao, iata, name, "Looking for airline in the database using ICAO and IATA codes");
            var airline = await _airlineManager.GetByCodeAsync(iata, icao);
            if ((airline == null) && !string.IsNullOrEmpty(name))
            {
                LogMessage(Severity.Info, icao, iata, name, "Looking for airline in the database by name");
                airline = await _airlineManager.GetAsync(x => x.Name == name);
                if (airline == null)
                {
                    LogMessage(Severity.Info, icao, iata, name, "Not stored locally, adding to the database");
                    airline = await _airlineManager.AddAsync(iata, icao, name);
                }
            }

            // If we've only got the codes, the airline could still be unidentified at this point, in which
            // case we need to use the API to look it up
            if (airline == null)
            {
                LogMessage(Severity.Info, icao, iata, name, "Not stored locally, using the API");

                // Get the API instance
                if (_register.GetInstance(ApiEndpointType.Airlines) is not IAirlinesApi api) return null;

                // Not stored locally, so use the API to look it up
                var properties = !string.IsNullOrEmpty(icao) ?
                    await api.LookupAirlineByICAOCodeAsync(icao) :
                    await api.LookupAirlineByICAOCodeAsync(iata);

                if ((properties?.Count ?? 0) > 0)
                {
                    // Extract the airline properties from the response
                    properties.TryGetValue(ApiProperty.AirlineICAO, out string airlineICAO);
                    properties.TryGetValue(ApiProperty.AirlineIATA, out string airlineIATA);
                    properties.TryGetValue(ApiProperty.AirlineName, out string airlineName);

                    // Create a new airline object containing the details returned by the API
                    LogMessage(Severity.Info, airlineICAO, airlineIATA, airlineName, "Saving new airline to the database");
                    airline = await _airlineManager.AddAsync(airlineIATA, airlineICAO, airlineName);
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
            => _logger.LogMessage(severity, $"Airline ICAO={icao}, IATA={iata}, Name={name} : {message}");
    }
}