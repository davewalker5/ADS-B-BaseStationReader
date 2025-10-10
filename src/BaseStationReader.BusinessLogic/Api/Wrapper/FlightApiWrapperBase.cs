using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    internal abstract class FlightApiWrapperBase
    {
        protected readonly ITrackerLogger _logger;
        protected readonly IAirlineApiWrapper _airlineWrapper;
        protected readonly IDatabaseManagementFactory _factory;

        public FlightApiWrapperBase(
            ITrackerLogger logger,
            IAirlineApiWrapper airlineWrapper,
            IDatabaseManagementFactory factory)
        {
            _logger = logger;
            _airlineWrapper = airlineWrapper;
            _factory = factory;
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
                properties[ApiProperty.FlightNumber],
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
        /// Return true if an airport code is allowed by the specified code list
        /// </summary>
        /// <param name="request"></param>
        /// <param name="type"></param>
        /// <param name="airportCode"></param>
        /// <returns></returns>
        protected bool IsAirportAllowed(ApiLookupRequest request, AirportType type, string airportCode)
        {
            var allowed = true;
            var airportCodeList = type == AirportType.Departure ? request.DepartureAirportCodes : request.ArrivalAirportCodes;
            var numberOfAirportCodes = airportCodeList?.Count();
            if (numberOfAirportCodes > 0)
            {
                allowed = airportCodeList.Contains(airportCode);
                var airportCodeListString = string.Join(", ", airportCodeList);
                var message = $"{type} code {airportCode} is in list {airportCodeListString} = {allowed}";
                LogMessage(Severity.Info, request, message);
            }
            else
            {
                LogMessage(Severity.Info, request, $"No {type} airport code filtering list supplied");
            }

            return allowed;
        }

        /// <summary>
        /// Log a message concerning a flight lookup
        /// </summary>
        /// <param name="severity"></param>
        /// <param name="request"></param>
        /// <param name="message"></param>
        protected void LogMessage(Severity severity, ApiLookupRequest request, string message)
            => _logger.LogMessage(severity, $"Flight lookup for aircraft {request.AircraftAddress} using {request.FlightPropertyType}={request.FlightPropertyValue} : {message}");
    }
}