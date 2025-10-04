using System.Globalization;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    internal class HistoricalFlightApiWrapper : FlightApiWrapperBase, IHistoricalFlightApiWrapper
    {
        private readonly IExternalApiRegister _register;
        private readonly ITrackedAircraftWriter _trackedAircraftWriter;

        public HistoricalFlightApiWrapper(
            ITrackerLogger logger,
            IExternalApiRegister register,
            IAirlineApiWrapper airlineWrapper,
            IFlightManager flightManager,
            ITrackedAircraftWriter trackedAircraftWriter) : base(logger, airlineWrapper, flightManager)
        {
            _register = register;
            _trackedAircraftWriter = trackedAircraftWriter;
        }

        /// <summary>
        /// Identify and save historical flight details for a tracked aircraft
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="propertyValue"></param>
        /// <param name="aircraftAddress"></param>
        /// <param name="date"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        public async Task<Flight> LookupFlightAsync(
            ApiProperty propertyType,
            string propertyValue,
            string aircraftAddress,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.HistoricalFlights) is not IHistoricalFlightsApi api) return null;

            // The aircraft address must be specified
            if (string.IsNullOrEmpty(propertyValue))
            {
                LogMessage(Severity.Warning, propertyType, propertyValue, aircraftAddress, "Invalid aircraft address for lookup");
                return null;
            }

            // Retrieve the tracked aircraft record 
            var aircraft = await _trackedAircraftWriter.GetAsync(x => (x.Address == aircraftAddress) && (x.LookupTimestamp == null));
            if (aircraft == null)
            {
                LogMessage(Severity.Warning, propertyType, propertyValue, aircraftAddress, $"Aircraft is not in the tracking table");
                return null;
            }

            // Use the API to look-up details for historical flights by the aircraft
            Flight flight = null;
            LogMessage(Severity.Info, propertyType, propertyValue, aircraftAddress, $"Looking up historical flights using the API");
            var properties = await api.LookupFlightsByAircraftAsync(aircraftAddress, aircraft.LastSeen);

            var numberOfFlights = properties?.Count ?? 0;
            LogMessage(Severity.Info, propertyType, propertyValue, aircraftAddress, $"{numberOfFlights} flight(s) found");

            if (numberOfFlights > 0)
            {
                // Iterate over the retrieved flight details
                foreach (var flightProperties in properties)
                {
                    // See if this one matches the filtering criteria
                    var matches = FilterFlight(propertyType, propertyValue, aircraft, flightProperties, departureAirportCodes, arrivalAirportCodes);
                    if (matches)
                    {
                        // Make sure the airline exists, as this is a pre-requisite for subsequently saving the flight
                        flightProperties.TryGetValue(ApiProperty.AirlineIATA, out string airlineIATA);
                        flightProperties.TryGetValue(ApiProperty.AirlineICAO, out string airlineICAO);
                        flightProperties.TryGetValue(ApiProperty.AirlineName, out string airlineName);
                        var airline = await _airlineWrapper.LookupAirlineAsync(airlineICAO, airlineIATA, airlineName);
                        if (airline != null)
                        {
                            // Save and return this flight as the matching flight
                            flight = await SaveFlight(flightProperties, airline.Id);
                            return flight;
                        }
                        else
                        {
                            LogMessage(Severity.Debug, propertyType, propertyValue, aircraftAddress, $"Unable to identify the airline - flight cannot be saved");
                            return null;
                        }
                    }
                }
            }

            // If we fall through to here, no matching flight's been found
            return null;
        }

        /// <summary>
        /// Determine if a property collection representing a flight matches a set of filtering criteria
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="propertyValue"></param>
        /// <param name="aircraft"></param>
        /// <param name="properties"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        private bool FilterFlight(
            ApiProperty propertyType,
            string propertyValue,
            TrackedAircraft aircraft,
            Dictionary<ApiProperty, string> properties,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Extract the departure airport code and see if the flight is filtered out
            var departure = properties[ApiProperty.EmbarkationIATA];
            if (!IsAirportAllowed(propertyType, propertyValue, aircraft.Address, AirportType.Departure, departure, departureAirportCodes))
            {
                return false;
            }
    
            // Extract the arrival airport code and see if the flight is filtered out
            var arrival = properties[ApiProperty.DestinationIATA];
            if (!IsAirportAllowed(propertyType, propertyValue, aircraft.Address, AirportType.Arrival, arrival, arrivalAirportCodes))
            {
                return false;
            }

            // Extract the address and check it matches
            // Update: The response doesn't always contain the address but as the request has been made for a specific
            // 24-bit ICAO address the assumption is that only matching flights are returned
            // var address = properties[ApiProperty.AircraftAddress];
            // if (address != aircraft.Address)
            // {
            //     LogMessage(Severity.Info, address, $"Address for route {departure} - {arrival} is {address} and does not match the expected address ({aircraft.Address})");
            //     return false;
            // }

            // Get the flight times from the properties collection
            var departureTime = ExtractTimestamp(properties[ApiProperty.DepartureTime]);
            var arrivalTime = ExtractTimestamp(properties[ApiProperty.ArrivalTime]);

            // Convert the last seen date on the aircraft to UTC and see if it passes the filters
            var lastSeenUtc = DateTime.SpecifyKind(aircraft.LastSeen, DateTimeKind.Local).ToUniversalTime();
            if (!CompareFlightTimes(propertyType, propertyValue, aircraft.Address, departureTime, arrivalTime, lastSeenUtc))
            {
                // The dates may have been returned as local time, marked as UTC in the response. Given the
                // difference can be a maximum of 1 hour and seeing the same aircraft on two flights in that
                // timeframe is unlikely, compare using local time as well
                if (!CompareFlightTimes(propertyType, propertyValue, aircraft.Address, departureTime, arrivalTime, aircraft.LastSeen))
                {
                    return false;
                }
            }

            LogMessage(Severity.Info, propertyType, propertyValue, aircraft.Address, $"Flight with route {departure} - {arrival} matches filters");
            return true;
        }

        /// <summary>
        /// Compare a last seen timestamp to the departure and arrival times for a flight
        /// </summary>
        /// <param name="propertyType"></param>
        /// <param name="propertyValue"></param>
        /// <param name="aircraftAddress"></param>
        /// <param name="departureTime"></param>
        /// <param name="arrivalTime"></param>
        /// <param name="lastSeen"></param>
        /// <returns></returns>
        private bool CompareFlightTimes(
            ApiProperty propertyType,
            string propertyValue,
            string aircraftAddress,
            DateTime? departureTime,
            DateTime? arrivalTime,
            DateTime lastSeen)
        {
            // Check the departure time has a value
            if (!departureTime.HasValue)
            {
                LogMessage(Severity.Info, propertyType, propertyValue, aircraftAddress, $"Departure time is not specified");
                return false;
            }

            // Departure time should be <= last seen
            if (departureTime.Value > lastSeen)
            {
                LogMessage(Severity.Info, propertyType, propertyValue, aircraftAddress, $"Departure time of {departureTime} is later than the observed time {lastSeen} {lastSeen.Kind}");
                return false;
            }

            // Check the arrival time has a value
            if (!arrivalTime.HasValue)
            {
                LogMessage(Severity.Info, propertyType, propertyValue, aircraftAddress, $"Arrival time is not specified");
                return false;
            }

            // Arrival time should be >= lastSeen
            if (arrivalTime.Value < lastSeen)
            {
                LogMessage(Severity.Info, propertyType, propertyValue, aircraftAddress, $"Arrival time of {arrivalTime} is earlier than the observed time {lastSeen} {lastSeen.Kind}");
                return false;
            }

            return true;
        }

        /// <summary>
        /// Parse a string representation of a UTC date and time to give the equivalent local date and time
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static DateTime? ExtractTimestamp(string value)
            => DateTime.TryParse(value, null, DateTimeStyles.AdjustToUniversal, out DateTime utc) ? utc : null;
    }
}