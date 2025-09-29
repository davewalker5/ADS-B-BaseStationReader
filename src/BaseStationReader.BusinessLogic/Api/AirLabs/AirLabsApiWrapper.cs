using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;
using BaseStationReader.Interfaces.Api;

namespace BaseStationReader.BusinessLogic.Api.AirLabs
{
    public class AirLabsApiWrapper : ApiWrapperBase, IApiWrapper
    {
        private const ApiServiceType ServiceType = ApiServiceType.AirLabs;

        protected IAirlinesApi _airlinesApi;
        protected IActiveFlightApi _flightsApi;

        /// <summary>
        /// Initialise the API wrapper
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="client"></param>
        /// <param name="apiConfiguration"></param>
        public bool Initialise(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            object context,
            ExternalApiSettings settings)
        {
            // Log the configuration properties
            logger.LogApiConfiguration(settings);

            // Cast the database context to the expected type
            if (context is not BaseStationReaderDbContext dbContext)
            {
                logger.LogMessage(Severity.Error, $"Invalid database context object");
                return false;
            }

            // Call the base class initialisation method
            base.Initialise(logger, client, dbContext);

            // Get the API configuration properties
            var definition = settings.ApiServices.FirstOrDefault(x => x.Service == ServiceType);
            var key = definition?.Key;
            var rateLimit = definition?.RateLimit ?? 0;

            var aircraftEndpointUrl = settings.ApiEndpoints.FirstOrDefault(x =>
                x.EndpointType == ApiEndpointType.Aircraft && x.Service == ServiceType)?.Url;

            var airlinesEndpointUrl = settings.ApiEndpoints.FirstOrDefault(x =>
                x.EndpointType == ApiEndpointType.Airlines && x.Service == ServiceType)?.Url;

            var flightsEndpointUrl = settings.ApiEndpoints.FirstOrDefault(x =>
                x.EndpointType == ApiEndpointType.ActiveFlights && x.Service == ServiceType)?.Url;

            // For the configuration to be valid, we need the endpoint URLs and the key
            bool valid = !string.IsNullOrEmpty(key) &&
                !string.IsNullOrEmpty(aircraftEndpointUrl) &&
                !string.IsNullOrEmpty(airlinesEndpointUrl) &&
                !string.IsNullOrEmpty(flightsEndpointUrl) &&
                (rateLimit >= 0);

            if (valid)
            {
                // Set the rate limit for this service on the HTTP client
                client.SetRateLimits(ServiceType, rateLimit);

                // Construct the API instances
                _airlinesApi = new AirLabsAirlinesApi(logger, client, airlinesEndpointUrl, key);
                _aircraftApi = new AirLabsAircraftApi(logger, client, aircraftEndpointUrl, key);
                _flightsApi = new AirLabsActiveFlightApi(logger, client, flightsEndpointUrl, key);
            }
            else
            {
                logger.LogMessage(Severity.Error, $"Invalid API configuration - missing endpoint URL(s) or key");
            }

            return valid;
        }

        /// <summary>
        /// Lookup all active flights within a bounding box around a central point
        /// </summary>
        /// <param name="centreLatitude"></param>
        /// <param name="centreLongitude"></param>
        /// <param name="rangeNm"></param>
        /// <returns></returns>
        public async Task<List<Flight>> LookupFlightsInBoundingBox(
            double centreLatitude,
            double centreLongitude,
            double rangeNm)
        {
            List<Flight> flights = [];

            // Use the API to look-up the flights
            var properties = await _flightsApi.LookupFlightsInBoundingBox(centreLatitude, centreLongitude, rangeNm);
            if ((properties != null) && (properties.Count > 0))
            {
                foreach (var flightDetails in properties)
                {
                    var flight = CreateFlightFromPropertyDictionary(flightDetails);
                    flights.Add(flight);
                }
            }

            return flights;
        }

        /// <summary>
        /// Lookup an active flight using the aircraft's ICAO 24-bit ICAO address
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        public async Task<Flight> LookupFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            Flight flight = null;

            // The aircraft address must be specified
            if (string.IsNullOrEmpty(address))
            {
                _logger.LogMessage(Severity.Warning, $"Unable to look up flight details : Invalid aircraft address");
                return null;
            }

            // Use the API to look-up the flight
            var properties = await _flightsApi.LookupFlightByAircraftAsync(address);
            if (properties != null)
            {
                // Extract the departure and arrival airport codes
                var departure = properties[ApiProperty.EmbarkationIATA];
                var arrival = properties[ApiProperty.DestinationIATA];

                _logger.LogMessage(Severity.Info, $"Found flight with route {departure} - {arrival} for aircraft {address}");

                // Check the codes against the filters
                var departureAllowed = departureAirportCodes?.Count() > 0 ? departureAirportCodes.Contains(departure) : true;
                var arrivalAllowed = arrivalAirportCodes?.Count() > 0 ? arrivalAirportCodes.Contains(arrival) : true;

                // Check both airports are found in the "allowed" lists
                if (departureAllowed && arrivalAllowed)
                {
                    // Create a new flight object containing the details returned by the API
                    flight = CreateFlightFromPropertyDictionary(properties);
                }
                else
                {
                    _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {address} excluded by the airport filters");
                }
            }

            return flight;
        }

        /// <summary>
        /// Lookup an active flight and store it
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        public override async Task<Flight> LookupAndStoreFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Request flight details for an active flight involving the aircraft with the specified ICAO address
            var flight = await LookupFlightAsync(address, departureAirportCodes, arrivalAirportCodes);
            if (flight != null)
            {
                // Get the airline details, storing them locally if not already present
                var airline = await LookupAndStoreAirlineAsync(flight.Airline.ICAO, flight.Airline.IATA);
                if (airline != null)
                {
                    // Capture the alternative ICAO for the aircraft model before saving as it's not a persisted
                    // property and will need to be restored afterwards
                    var alternateModelICAO = flight.ModelICAO;

                    // Airline details have been retrieved OK so create the flight (the flight manager prevents creation
                    // of duplicates)
                    flight = await _flightManager.AddAsync(flight.IATA, flight.ICAO, flight.Number, flight.Embarkation, flight.Destination, airline.Id);
                    flight.ModelICAO = alternateModelICAO;
                }
            }

            return flight;
        }

        /// <summary>
        /// Retrieve or look up and aircraft given it's ICAO and/or IATA code
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <returns></returns>
        public async Task<Airline> LookupAirlineAsync(string icao, string iata)
        {
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
                    await _airlinesApi.LookupAirlineByICAOCodeAsync(icao) :
                    await _airlinesApi.LookupAirlineByICAOCodeAsync(iata);

                if (properties != null)
                {
                    // Create a new airline object containing the details returned by the API
                    airline = new()
                    {
                        IATA = properties[ApiProperty.AirlineIATA],
                        ICAO = properties[ApiProperty.AirlineICAO],
                        Name = properties[ApiProperty.AirlineName]
                    };
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

        /// <summary>
        /// Retrieve or lookup an airline, making sure it's saved locally
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <returns></returns>
        public async Task<Airline> LookupAndStoreAirlineAsync(string icao, string iata)
        {
            // Attempt to load the airline based on its ICAO or IATA code
            var airline = await LookupAirlineAsync(icao, iata);
            if ((airline != null) && (airline.Id == 0))
            {
                // Airline was found but was not loaded from the database, so save it
                airline = await _airlineManager.AddAsync(airline.IATA, airline.ICAO, airline.Name);
            }

            return airline;
        }
    }
}
