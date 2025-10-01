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
        private readonly ITrackerLogger _logger;
        private readonly IExternalApiRegister _register;

        public ActiveFlightApiWrapper(
            ITrackerLogger logger,
            IExternalApiRegister register,
            IAirlineManager airlineManager,
            IFlightManager flightManager) : base(airlineManager, flightManager)
        {
            _logger = logger;
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
                _logger.LogMessage(Severity.Warning, $"Unable to look up flight details : Invalid aircraft address");
                return null;
            }

            // Use the API to look-up the flight
            Flight flight = null;
            var properties = await api.LookupFlightByAircraftAsync(address);
            if (properties?.Count > 0)
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
                    flight = await SaveFlight(properties);
                }
                else
                {
                    _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {address} excluded by the airport filters");
                }
            }

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
            if (properties?.Count > 0)
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