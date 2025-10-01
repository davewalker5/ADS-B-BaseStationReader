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
        /// <returns></returns>
        public async Task<Airline> LookupAirlineAsync(string icao, string iata)
        {
            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.Airlines) is not IAirlinesApi api) return null;

            // At least one of the parameters must be specified
            if (string.IsNullOrEmpty(icao) && string.IsNullOrEmpty(iata))
            {
                _logger.LogMessage(Severity.Warning, $"Unable to look up airline details : Invalid ICAO and IATA codes");
                return null;
            }
            // See if the airline is stored locally, first
            var airline = await _airlineManager.GetByCodeAsync(iata, icao);
            if (airline == null)
            {
                _logger.LogMessage(Severity.Info, $"Airline with ICAO = '{icao}', IATA = '{iata}' is not stored locally : Using the API");

                // Not stored locally, so use the API to look it up
                var properties = !string.IsNullOrEmpty(icao) ?
                    await api.LookupAirlineByICAOCodeAsync(icao) :
                    await api.LookupAirlineByICAOCodeAsync(iata);

                if (properties?.Count > 0)
                {
                    // Create a new airline object containing the details returned by the API
                    airline = await _airlineManager.AddAsync(
                        properties[ApiProperty.AirlineIATA],
                        properties[ApiProperty.AirlineICAO],
                        properties[ApiProperty.AirlineName]);
                }
                else
                {
                    _logger.LogMessage(Severity.Info, $"API lookup for Airline with ICAO = '{icao}', IATA = '{iata}' produced no results");
                }
            }
            else
            {
                _logger.LogMessage(Severity.Info, $"Airline with ICAO = '{icao}', IATA = '{iata}' retrieved from the database");
            }

            return airline;
        }

    }
}