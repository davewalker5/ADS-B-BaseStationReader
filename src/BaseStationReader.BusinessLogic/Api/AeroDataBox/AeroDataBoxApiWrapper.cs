using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Interfaces.Tracking;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.AeroDatabox
{
    public class AeroDataBoxApiWrapper: ApiWrapperBase, IApiWrapper
    {
        private const ApiServiceType ServiceType = ApiServiceType.AeroDataBox;

        private ITrackedAircraftWriter _trackedAircraftWriter;
        private IHistoricalFlightApi _flightsApi;

        /// <summary>
        /// Initialise the API wrapper
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="client"></param>
        /// <param name="context"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
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

            var flightsEndpointUrl = settings.ApiEndpoints.FirstOrDefault(x =>
                x.EndpointType == ApiEndpointType.HistoricalFlights && x.Service == ServiceType)?.Url;

            // For the configuration to be valid, we need the endpoint URLs and the key
            bool valid = !string.IsNullOrEmpty(key) &&
                !string.IsNullOrEmpty(aircraftEndpointUrl) &&
                !string.IsNullOrEmpty(flightsEndpointUrl) &&
                (rateLimit >= 0);

            if (valid)
            {
                // Set the rate limit for this service on the HTTP client
                client.SetRateLimits(ServiceType, rateLimit);

                // Construct the API instances
                _aircraftApi = new AeroDataBoxAircraftApi(logger, client, aircraftEndpointUrl, key);
                _flightsApi = new AeroDataBoxHistoricalFlightApi(logger, client, flightsEndpointUrl, key);

                // Construct the tracked aircraft manager
                _trackedAircraftWriter = new TrackedAircraftWriter(dbContext);
            }
            else
            {
                _logger.LogMessage(Severity.Error, $"Invalid API configuration - missing endpoint URL(s) or key");
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
#pragma warning disable CS1998
        public async Task<List<Flight>> LookupFlightsInBoundingBox(
            double centreLatitude,
            double centreLongitude,
            double rangeNm)
            => throw new NotImplementedException();
#pragma warning restore CS1998

        /// <summary>
        /// Lookup a historical flight using the aircraft's ICAO 24-bit ICAO address
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

            // Retrieve the tracked aircraft record 
            var aircraft = await _trackedAircraftWriter.GetAsync(x => x.Address == address);
            if (aircraft == null)
            {
                _logger.LogMessage(Severity.Warning, $"Unable to look up flight details : Aircraft with address '{address}' not found");
                return null;
            }

            _logger.LogMessage(Severity.Info, $"Looking up historical flights for aircraft with address '{address}'");

            // Use the API to look-up the flight
            var properties = await _flightsApi.LookupFlightsByAircraftAsync(address);
            if (properties != null)
            {
                foreach (var flightProperties in properties)
                {
                    // Check the properties against the filters - this will return a flight object if 
                    flight = FilterAndCreateFlight(aircraft, flightProperties, departureAirportCodes, arrivalAirportCodes);
                    if (flight != null)
                    {
                        return flight;
                    }
                }
            }

            // If we fall through to here, no matching flight's been found
            return null;
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
                // Store the airline in the database
                var airline = await StoreAirlineAsync(flight.Airline.ICAO, flight.Airline.IATA, flight.Airline.Name);

                // Airline details have been retrieved OK so create the flight (the flight manager prevents creation
                // of duplicates)
                flight = await _flightManager.AddAsync(flight.IATA, flight.ICAO, flight.Number, flight.Embarkation, flight.Destination, airline.Id);
            }

            return flight;
        }

        /// <summary>
        /// Retrieve or look up and aircraft given it's ICAO and/or IATA code. For AeroDataBox, the airline is returned
        /// and stored as part of the flight lookup so this is just a data retrieval
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <returns></returns>
        public async Task<Airline> LookupAirlineAsync(string icao, string iata)
            => await LoadAirline(icao, iata);

        /// <summary>
        /// Retrieve or lookup an airline, making sure it's saved locally. For AeroDataBox, the airline is returned
        /// and stored as part of the flight lookup so this is just a data retrieval
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <returns></returns>
        public async Task<Airline> LookupAndStoreAirlineAsync(string icao, string iata)
            => await LoadAirline(icao, iata);

        /// <summary>
        /// Load an airline from the database using ICAO or IATA code
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <returns></returns>
        private async Task<Airline> LoadAirline(string icao, string iata)
            => !string.IsNullOrEmpty(iata) ?
                await _airlineManager.GetAsync(x => x.IATA == iata) :
                await _airlineManager.GetAsync(x => x.ICAO == icao);

        /// <summary>
        /// Store an airline in the database
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <param name="name"></param>
        /// <returns></returns>
        private async Task<Airline> StoreAirlineAsync(string icao, string iata, string name)
        {
            // See if the airline's already in the database
            var airline = await LoadAirline(icao, iata);
            if (airline == null)
            {
                _logger.LogMessage(Severity.Info, $"Adding airline with ICAO = '{icao}', IATA = '{iata}' to the database");
                airline = await _airlineManager.AddAsync(iata, icao, name);
            }
            else
            {
                _logger.LogMessage(Severity.Info, $"Airline with ICAO = '{icao}', IATA = '{iata}' is already in the database");
            }

            return airline;
        }

        /// <summary>
        /// Check a set of flight properties pass the filtering criteria and, if so, create and return a flight object
        /// instance
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="properties"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        private Flight FilterAndCreateFlight(
            TrackedAircraft aircraft,
            Dictionary<ApiProperty, string> properties,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Extract the departure and arrival airport codes
            var departure = properties[ApiProperty.EmbarkationIATA];
            var arrival = properties[ApiProperty.DestinationIATA];

            // Extract the address and check it matches
            var address = properties[ApiProperty.AircraftAddress];
            if (address != aircraft.Address)
            {
                _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {address} does not have the correct aircraft address");
                return null;
            }

            // Check the airport codes against the filters
            var departureAllowed = !(departureAirportCodes?.Count() > 0) || departureAirportCodes.Contains(departure);
            var arrivalAllowed = !(arrivalAirportCodes?.Count() > 0) || arrivalAirportCodes.Contains(arrival);

            if (!departureAllowed || !arrivalAllowed)
            {
                _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {aircraft.Address} excluded by the airport filters");
                return null;
            }

            // Extract the departure and arrival times
            var departureTime = ExtractTimestamp(properties[ApiProperty.DepartureTime]);
            var arrivalTime = ExtractTimestamp(properties[ApiProperty.ArrivalTime]);

            // Check the flight times include the last seen date on the aircraft
            if (!departureTime.HasValue || (departureTime.Value > aircraft.LastSeen) ||
                !arrivalTime.HasValue || (arrivalTime.Value < aircraft.LastSeen))
            {
                _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {aircraft.Address} excluded by the flight time filters");
                return null;
            }

            _logger.LogMessage(Severity.Info, $"Found flight with route {departure} - {arrival} for aircraft {aircraft.Address}");

            // We have a flight that passes the filtering criteria - create a flight object from the properties
            var flight = CreateFlightFromPropertyDictionary(properties);
            return flight;
        }
    }
}