using System.Globalization;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;

namespace BaseStationReader.Api.Wrapper
{
    internal class FlightLookupManager : IFlightLookupManager
    {
        private readonly IExternalApiRegister _register;
        private readonly IDatabaseManagementFactory _factory;
        private readonly IAirlineLookupManager _airlineLookupManager;

        public FlightLookupManager(IExternalApiRegister register, IDatabaseManagementFactory factory, IAirlineLookupManager airlineLookupManager)
        {
            _register = register;
            _factory = factory;
            _airlineLookupManager = airlineLookupManager;
        }

        /// <summary>
        /// Identify a flight from an aircraft address
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        public async Task<Flight> IdentifyFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Attempt to load the flight from the database or, if there's a callsign mapping for it, to create
            // a new one using that mapping
            (TrackedAircraft aircraft, Flight flight) = await LoadOrCreateFlightAsync(address, departureAirportCodes, arrivalAirportCodes);

            // At this point, one of the following is true:
            //
            // 1. The flight is a pre-existing flight or;
            // 2. There's a callsign mapping record for it, in which case it's been created based on that record or;
            // 3. The flight is still unidentified
            //
            // With no mapping record, there's no way to identify a flight number so APIs supporting lookup by flight
            // number are of no use at this point. We now need an API that is able to lookup flights by aircraft
            // address - attempt that lookup
            if ((aircraft != null) && (flight == null))
            {
                flight = await LookupFlightAsync(aircraft, departureAirportCodes, arrivalAirportCodes);
            }

            // Log the flight details
            if (flight != null)
            {
                LogFlightDetails(address, flight);
            }

            return flight;
        }

        /// <summary>
        /// Attempt to load a flight from the database
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        private async Task<(TrackedAircraft, Flight)> LoadOrCreateFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Load the aircraft tracking record for the specified aircraft address
            LogMessage(Severity.Info, address, $"Attempting to retrieve the tracking record from the database");
            var trackedAircraft = await _factory.TrackedAircraftWriter.GetAsync(x => (x.Address == address) && (x.Status != TrackingStatus.Inactive));
            if (trackedAircraft == null)
            {
                LogMessage(Severity.Info, address, $"No tracking record found");
                return (null, null);
            }

            // See if there's a callsign
            if (string.IsNullOrEmpty(trackedAircraft.Callsign))
            {
                LogMessage(Severity.Info, address, $"Tracking record does not contain a valid callsign");
                return (trackedAircraft, null);
            }

            // Attempt to load the callsign mapping record
            LogMessage(Severity.Info, address, trackedAircraft.Callsign, $"Attempting to retrieve callsign mapping record");
            var mapping = await _factory.FlightIATACodeMappingManager.GetAsync(x => x.Callsign == trackedAircraft.Callsign);
            if (mapping == null)
            {
                LogMessage(Severity.Info, address, trackedAircraft.Callsign, $"No callsign mapping record found");
                return (trackedAircraft, null);
            }

            // Check the point of embarkation isn't filtered out
            if (!IsAirportAllowed(trackedAircraft.Address, departureAirportCodes, AirportType.Departure, mapping.Embarkation))
            {
                return (trackedAircraft, null);
            }

            // Check the destination isn't filtered out
            if (!IsAirportAllowed(trackedAircraft.Address, arrivalAirportCodes, AirportType.Arrival, mapping.Destination))
            {
                return (trackedAircraft, null);
            }

            // See if the flight already exists
            LogMessage(Severity.Info, address, trackedAircraft.Callsign, mapping.FlightIATA, $"Attempting to retrieve the flight from the database");
            var flight = await _factory.FlightManager.GetAsync(x => x.IATA == mapping.FlightIATA);
            if (flight != null)
            {
                return (trackedAircraft, flight);
            }

            // At this point, we have all the information necessary to create the airline and flight from the mapping record
            var airline = await _factory.AirlineManager.AddAsync(mapping.AirlineIATA, mapping.AirlineICAO, mapping.AirlineName);
            flight = await _factory.FlightManager.AddAsync(mapping.FlightIATA, null, mapping.Embarkation, mapping.Destination, airline.Id);
                return (trackedAircraft, flight);
        }

        /// <summary>
        /// Lookup the flight based on the aircraft address
        /// </summary>
        /// <param name="aircraft"></param>
        /// <returns></returns>
        private async Task<Flight> LookupFlightAsync(
            TrackedAircraft aircraft,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            Flight flight = null;

            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.Flights) is not IFlightApi api)
            {
                LogMessage(Severity.Error, aircraft.Address, $"Registered flights API is not an instance of {typeof(IFlightApi).Name}");
                return null;
            }

            LogMessage(Severity.Info, aircraft?.Address, $"Using the {api.GetType().Name} API to look up flight details");

            // Lookup flights for this aircraft - this may return multiple flights
            var properties = await api.LookupFlightsAsync(aircraft.Address, aircraft.LastSeen);
            var numberOfFlights = properties?.Count ?? 0;
            LogMessage(Severity.Info, aircraft.Address, $"{numberOfFlights} flight(s) found");

            if (numberOfFlights > 0)
            {
                // Iterate over the retrieved flight details
                foreach (var flightProperties in properties)
                {
                    // Get the flight times from the properties collection
                    var departureTime = ExtractTimestamp(flightProperties, ApiProperty.DepartureTime);
                    var arrivalTime = ExtractTimestamp(flightProperties, ApiProperty.ArrivalTime);

                    // If the times are specified then we filter to check the flight matches the tracking time. If not,
                    // just assume it's the right flight
                    var matches = (departureTime == null) || (arrivalTime == null) || FilterFlight(aircraft, flightProperties, departureTime, arrivalTime, departureAirportCodes, arrivalAirportCodes);
                    if (matches)
                    {
                        // Make sure the airline exists, as this is a pre-requisite for subsequently saving the flight
                        flightProperties.TryGetValue(ApiProperty.AirlineIATA, out string airlineIATA);
                        flightProperties.TryGetValue(ApiProperty.AirlineICAO, out string airlineICAO);
                        flightProperties.TryGetValue(ApiProperty.AirlineName, out string airlineName);
                        var airline = await _airlineLookupManager.IdentifyAirlineAsync(airlineIATA, airlineICAO, airlineName);
                        if (airline != null)
                        {
                            // Save and return this flight as the matching flight
                            flight = await SaveFlightAsync(flightProperties, airline.Id);
                            return flight;
                        }
                        else
                        {
                            LogMessage(Severity.Debug, aircraft.Address, $"Unable to identify the airline - flight cannot be saved");
                            return null;
                        }
                    }
                }
            }

            return flight;
        }

        /// <summary>
        /// Determine if a property collection representing a flight matches a set of filtering criteria
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="properties"></param>
        /// <param name="departureTime"></param>
        /// <param name="arrivalTime"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        private bool FilterFlight(
            TrackedAircraft aircraft,
            Dictionary<ApiProperty, string> properties,
            DateTime? departureTime,
            DateTime? arrivalTime,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Extract the departure airport code and see if the flight is filtered out
            var departure = properties[ApiProperty.EmbarkationIATA];
            if (!IsAirportAllowed(aircraft.Address, departureAirportCodes, AirportType.Departure, departure))
            {
                return false;
            }
    
            // Extract the arrival airport code and see if the flight is filtered out
            var arrival = properties[ApiProperty.DestinationIATA];
            if (!IsAirportAllowed(aircraft.Address, arrivalAirportCodes, AirportType.Arrival, arrival))
            {
                return false;
            }

            // Convert the last seen date on the aircraft to UTC and see if it passes the filters
            var lastSeenUtc = DateTime.SpecifyKind(aircraft.LastSeen, DateTimeKind.Local).ToUniversalTime();
            if (!CompareFlightTimes(aircraft.Address, departureTime, arrivalTime, lastSeenUtc))
            {
                // The dates may have been returned as local time, marked as UTC in the response. Given the
                // difference can be a maximum of 1 hour and seeing the same aircraft on two flights in that
                // timeframe is unlikely, compare using local time as well
                if (!CompareFlightTimes(aircraft.Address, departureTime, arrivalTime, aircraft.LastSeen))
                {
                    return false;
                }
            }

            LogMessage(Severity.Info, aircraft.Address, $"Flight with route {departure} - {arrival} matches filters");
            return true;
        }

        /// <summary>
        /// Return true if an airport code is allowed by the specified code list
        /// </summary>
        /// <param name="address"></param>
        /// <param name="airportCodeList"></param>
        /// <param name="type"></param>
        /// <param name="airportCode"></param>
        /// <returns></returns>
        protected bool IsAirportAllowed(string address, IEnumerable<string> airportCodeList, AirportType type, string airportCode)
        {
            var allowed = true;
            var numberOfAirportCodes = airportCodeList?.Count();
            if (numberOfAirportCodes > 0)
            {
                allowed = airportCodeList.Contains(airportCode);
                var airportCodeListString = string.Join(", ", airportCodeList);
                var message = $"{type} code {airportCode} is in list {airportCodeListString} = {allowed}";
                LogMessage(Severity.Info, address, message);
            }
            else
            {
                LogMessage(Severity.Info, address, $"No {type} airport code filtering list supplied");
            }

            return allowed;
        }

        /// <summary>
        /// Compare a last seen timestamp to the departure and arrival times for a flight
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureTime"></param>
        /// <param name="arrivalTime"></param>
        /// <param name="lastSeen"></param>
        /// <returns></returns>
        private bool CompareFlightTimes(string address, DateTime? departureTime, DateTime? arrivalTime, DateTime lastSeen)
        {
            // Check the departure time has a value
            if (!departureTime.HasValue)
            {
                LogMessage(Severity.Info, address, $"Departure time is not specified");
                return false;
            }

            // Departure time should be <= last seen
            if (departureTime.Value > lastSeen)
            {
                LogMessage(Severity.Info, address, $"Departure time of {departureTime} is later than the observed time {lastSeen} {lastSeen.Kind}");
                return false;
            }

            // Check the arrival time has a value
            if (!arrivalTime.HasValue)
            {
                LogMessage(Severity.Info, address, $"Arrival time is not specified");
                return false;
            }

            // Arrival time should be >= lastSeen
            if (arrivalTime.Value < lastSeen)
            {
                LogMessage(Severity.Info, address, $"Arrival time of {arrivalTime} is earlier than the observed time {lastSeen} {lastSeen.Kind}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Given a set of API property values representing a flight, create and save a new flight
        /// locally
        /// </summary>
        /// <param name="properties"></param>
        /// <param name="airlineId"></param>
        /// <returns></returns>
        protected async Task<Flight> SaveFlightAsync(Dictionary<ApiProperty, string> properties, int airlineId)
        {
            // Save the flight
            Flight flight = await _factory.FlightManager.AddAsync(
                properties[ApiProperty.FlightIATA],
                properties[ApiProperty.FlightICAO],
                properties[ApiProperty.EmbarkationIATA],
                properties[ApiProperty.DestinationIATA],
                airlineId);

            // There may be additional aircraft details in the flight properties
            properties.TryGetValue(ApiProperty.AircraftAddress, out string address);
            properties.TryGetValue(ApiProperty.ModelICAO, out string modelICAO);
            flight.AircraftAddress = address;
            flight.ModelICAO = modelICAO;

            // And as we now have a matching flight, return it
            return flight;
        }

        /// <summary>
        /// Parse a string representation of a UTC date and time to give the equivalent local date and time
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static DateTime? ExtractTimestamp(Dictionary<ApiProperty, string> properties, ApiProperty property)
        {
            properties.TryGetValue(property, out string value);
            return DateTime.TryParse(value, null, DateTimeStyles.AdjustToUniversal, out DateTime utc) ? utc : null;
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
        /// Output a message formatted with the aircraft address and callsign
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="address"></param>
        /// <param name="message"></param>
        private void LogMessage(Severity severity, string address, string callsign, string message)
            => _factory.Logger.LogMessage(severity, $"Aircraft '{address}', callsign '{callsign}': {message}");

        /// <summary>
        /// Output a message formatted with the aircraft address and callsign
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="address"></param>
        /// <param name="message"></param>
        private void LogMessage(Severity severity, string address, string callsign, string iata, string message)
            => _factory.Logger.LogMessage(severity, $"Aircraft '{address}', callsign '{callsign}', flight IATA '{iata}': {message}");

        /// <summary>
        /// Log the details for a flight
        /// </summary>
        /// <param name="flight"></param>
        private void LogFlightDetails(string address, Flight flight)
            => LogMessage(Severity.Info, address, 
                $"Identified flight: " +
                $"IATA = {flight.IATA}, " +
                $"ICAO = {flight.ICAO}, " +
                $"Embarkation = {flight.Embarkation}, " +
                $"Destination = {flight.Destination}, " +
                $"Airline = {flight.Airline.Name}");
    }
}