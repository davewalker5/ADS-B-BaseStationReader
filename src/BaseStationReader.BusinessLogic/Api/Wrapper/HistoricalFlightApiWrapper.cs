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
        private readonly ITrackerLogger _logger;
        private readonly IExternalApiRegister _register;
        private readonly ITrackedAircraftWriter _trackedAircraftWriter;

        public HistoricalFlightApiWrapper(
            ITrackerLogger logger,
            IExternalApiRegister register,
            IAirlineManager airlineManager,
            IFlightManager flightManager,
            ITrackedAircraftWriter trackedAircraftWriter) : base(airlineManager, flightManager)
        {
            _logger = logger;
            _register = register;
            _trackedAircraftWriter = trackedAircraftWriter;
        }

        /// <summary>
        /// Identify and save historical flight details for a tracked aircraft
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
            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.HistoricalFlights) is not IHistoricalFlightsApi api) return null;

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
                _logger.LogMessage(Severity.Warning, $"Unable to look up flight details : Aircraft with address '{address}' is not being tracked");
                return null;
            }

            _logger.LogMessage(Severity.Info, $"Looking up historical flights for aircraft with address '{address}'");

            // Use the API to look-up details for historical flights by the aircraft
            Flight flight = null;
            var properties = await api.LookupFlightsByAircraftAsync(address);
            if (properties?.Count > 0)
            {
                foreach (var flightProperties in properties)
                {
                    // See if this one matches the filtering criteria
                    var matches = FilterFlight(aircraft, flightProperties, departureAirportCodes, arrivalAirportCodes);
                    if (matches)
                    {
                        // Save the airline and the flight
                        var airline = await _airlineManager.AddAsync(
                            flightProperties[ApiProperty.AirlineIATA], flightProperties[ApiProperty.AirlineICAO], flightProperties[ApiProperty.AirlineName]);

                        flight = await SaveFlight(flightProperties);

                        // And as we now have a matching flight, return it
                        return flight;
                    }
                }
            }

            // If we fall through to here, no matching flight's been found
            return null;
        }
        

        /// <summary>
        /// Determine if a property collection representing a flight matches a set of filtering criteria
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="properties"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        private bool FilterFlight(
            TrackedAircraft aircraft,
            Dictionary<ApiProperty, string> properties,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Extract the departure and arrival airport codes
            var departure = properties[ApiProperty.EmbarkationIATA];
            var arrival = properties[ApiProperty.DestinationIATA];

            _logger.LogMessage(Severity.Info, $"Checking flight with route {departure} - {arrival} against the filters for aircraft {aircraft.Address}");

            // Extract the address and check it matches
            var address = properties[ApiProperty.AircraftAddress];
            if (address != aircraft.Address)
            {
                _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {address} does not have the correct aircraft address");
                return false;
            }

            // Check the airport codes against the filters
            var departureAllowed = !(departureAirportCodes?.Count() > 0) || departureAirportCodes.Contains(departure);
            var arrivalAllowed = !(arrivalAirportCodes?.Count() > 0) || arrivalAirportCodes.Contains(arrival);

            if (!departureAllowed || !arrivalAllowed)
            {
                _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {aircraft.Address} excluded by the airport filters");
                return false;
            }

            // Extract the departure and arrival times
            var departureTime = ExtractTimestamp(properties[ApiProperty.DepartureTime]);
            var arrivalTime = ExtractTimestamp(properties[ApiProperty.ArrivalTime]);

            // Check the flight times include the last seen date on the aircraft
            if (!departureTime.HasValue || (departureTime.Value > aircraft.LastSeen) ||
                !arrivalTime.HasValue || (arrivalTime.Value < aircraft.LastSeen))
            {
                _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {aircraft.Address} excluded by the flight time filters");
                return false;
            }

            _logger.LogMessage(Severity.Info, $"Flight with route {departure} - {arrival} matches filters for aircraft {aircraft.Address}");
            return true;
        }

        /// <summary>
        /// Parse a string representation of a UTC date and time to give the equivalent local date and time
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static DateTime? ExtractTimestamp(string value)
            => DateTime.TryParse(value, null, DateTimeStyles.AdjustToUniversal, out DateTime utc) ? utc.ToLocalTime() : null;
    }
}