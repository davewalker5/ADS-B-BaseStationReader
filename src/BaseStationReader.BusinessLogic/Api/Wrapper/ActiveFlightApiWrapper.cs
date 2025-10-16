using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
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
            IDatabaseManagementFactory factory) : base(logger, airlineWrapper, factory)
        {
            _register = register;
        }

        /// <summary>
        /// Return true if the API implementation supports flight lookup by the specified property
        /// </summary>
        /// <param name="propertyType"></param>
        /// <returns></returns>
        public bool SupportsLookupBy(ApiProperty propertyType)
        {
            if (_register.GetInstance(ApiEndpointType.ActiveFlights) is not IActiveFlightsApi api) return false;
            return api.SupportsLookupBy(propertyType);
        }

        /// <summary>
        /// Look up an active flight and store it locally
        /// </summary>
        /// <param name="request"></param>
        /// <returns></returns>
        public async Task<Flight> LookupFlightAsync(ApiLookupRequest request)
        {
            // Get the API instance
            if (_register.GetInstance(ApiEndpointType.ActiveFlights) is not IActiveFlightsApi api) return null;

            // The property type must be supported
            if (!api.SupportsLookupBy(request.FlightPropertyType))
            {
                LogMessage(Severity.Warning, request, $"Flight lookup by {request.FlightPropertyType} is not supported");
                return null;
            }

            // The property value must be specified
            if (string.IsNullOrEmpty(request.FlightPropertyValue))
            {
                LogMessage(Severity.Warning, request, "Invalid property value for lookup");
                return null;
            }

            // Use the API to look-up the flight
            var properties = await api.LookupFlightAsync(request.FlightPropertyType, request.FlightPropertyValue);
            if ((properties?.Count ?? 0) == 0)
            {
                return null;
            }

            // Extract the departure airport code and see if the flight is filtered out
            var departure = properties[ApiProperty.EmbarkationIATA];
            if (!IsAirportAllowed(request, AirportType.Departure, departure))
            {
                return null;
            }

            // Extract the arrival airport code and see if the flight is filtered out
            var arrival = properties[ApiProperty.DestinationIATA];
            if (!IsAirportAllowed(request, AirportType.Arrival, arrival))
            {
                return null;
            }

            LogMessage(Severity.Info, request, $"Route {departure} - {arrival} passes the airport filters");

            // Make sure the airline exists, as this is a pre-requisite for subsequently saving the flight
            properties.TryGetValue(ApiProperty.AirlineIATA, out string airlineIATA);
            properties.TryGetValue(ApiProperty.AirlineICAO, out string airlineICAO);
            properties.TryGetValue(ApiProperty.AirlineName, out string airlineName);
            var airline = await _airlineWrapper.LookupAirlineAsync(airlineICAO, airlineIATA, airlineName);
            if (airline == null)
            {
                LogMessage(Severity.Info, request, $"Unable to identify the airline - flight cannot be saved");
                return null;
            }

            // Create a new flight object containing the details returned by the API
            Flight flight = await SaveFlightAsync(properties, airline.Id);
            return flight;
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
    }
}