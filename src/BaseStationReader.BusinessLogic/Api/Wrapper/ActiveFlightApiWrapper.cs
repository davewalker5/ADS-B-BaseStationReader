using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    internal class ActiveFlightApiWrapper : FlightApiWrapperBase, IActiveFlightApiWrapper
    {
        private readonly IExternalApiRegister _register;

        public ActiveFlightApiWrapper(
            ITrackerLogger logger,
            IExternalApiRegister register,
            IAirlineApiWrapper airlineWrapper,
            IFlightManager flightManager) : base(logger, airlineWrapper, flightManager)
        {
            _register = register;
        }

        /// <summary>
        /// Look up an active flight and store it locally
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirports"></param>
        /// <param name="arrivalAirports"></param>
        /// <returns></returns>
        public async Task<Flight> LookupFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.ActiveFlights) is not IActiveFlightsApi api) return null;

            // The aircraft address must be specified
            if (string.IsNullOrEmpty(address))
            {
                LogMessage(Severity.Warning, address, "Invalid aircraft address for lookup");
                return null;
            }

            // Use the API to look-up the flight
            var properties = await api.LookupFlightByAircraftAsync(address);
            if ((properties?.Count ?? 0) == 0)
            {
                return null;
            }

            // Extract the departure airport code and see if the flight is filtered out
            var departure = properties[ApiProperty.EmbarkationIATA];
            if (!IsAirportAllowed(address, AirportType.Departure, departure, departureAirportCodes))
            {
                return null;
            }

            // Extract the arrival airport code and see if the flight is filtered out
            var arrival = properties[ApiProperty.DestinationIATA];
            if (!IsAirportAllowed(address, AirportType.Arrival, departure, departureAirportCodes))
            {
                return null;
            }

            LogMessage(Severity.Info, address, $"Route {departure} - {arrival} passes the airport filters");

            // Make sure the airline exists, as this is a pre-requisite for subsequently saving the flight
            properties.TryGetValue(ApiProperty.AirlineIATA, out string airlineIATA);
            properties.TryGetValue(ApiProperty.AirlineICAO, out string airlineICAO);
            properties.TryGetValue(ApiProperty.AirlineName, out string airlineName);
            var airline = await _airlineWrapper.LookupAirlineAsync(airlineICAO, airlineIATA, airlineName);
            if (airline == null)
            {
                LogMessage(Severity.Info, address, $"Unable to identify the airline - flight cannot be saved");
                return null;
            }

            // Create a new flight object containing the details returned by the API
            Flight flight = await SaveFlight(properties, airline.Id);
            return flight;
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
            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.ActiveFlights) is not IActiveFlightsApi api) return null;

            List<Flight> flights = [];

            // Use the API to look-up the flights
            var properties = await api.LookupFlightsInBoundingBox(centreLatitude, centreLongitude, rangeNm);
            if ((properties?.Count ?? 0) > 0)
            {
                // Iterate over the collection of flight properties
                foreach (var flightDetails in properties)
                {
                    // Create a flight object from this set of properties and add it to the collection
                    var flight = CreateFlightFromProperties(flightDetails);
                    flights.Add(flight);
                }
            }

            return flights;
        }

        /// <summary>
        /// Return true if we have a valid value for an API property
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="key"></param>
        /// <returns></returns>
        protected static bool HaveValue(Dictionary<ApiProperty, string> properties, ApiProperty key)
        {
            var value = properties?.ContainsKey(key) == true ? properties[key] : null;
            return !string.IsNullOrEmpty(value);
        }

        /// <summary>
        /// Create a flight object from a dictionary of properties returned from the API
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private static Flight CreateFlightFromProperties(Dictionary<ApiProperty, string> properties)
            => new()
            {
                Embarkation = properties[ApiProperty.EmbarkationIATA],
                Destination = properties[ApiProperty.DestinationIATA],
                IATA = properties[ApiProperty.FlightIATA],
                ICAO = properties[ApiProperty.FlightICAO],
                Number = properties[ApiProperty.FlightNumber],
                Airline = new()
                {
                    IATA = properties[ApiProperty.AirlineIATA],
                    ICAO = properties[ApiProperty.AirlineICAO],
                    Name = properties[ApiProperty.AirlineName]
                },
                AircraftAddress = properties[ApiProperty.AircraftAddress],
                ModelICAO = properties[ApiProperty.ModelICAO]
            };
    }
}