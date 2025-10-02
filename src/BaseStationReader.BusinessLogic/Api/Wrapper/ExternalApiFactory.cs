using BaseStationReader.BusinessLogic.Api.AeroDatabox;
using BaseStationReader.BusinessLogic.Api.AirLabs;
using BaseStationReader.BusinessLogic.Api.CheckWXApi;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    public static class ExternalApiFactory
    {
        /// <summary>
        /// Build a dictionary where the key is the string representation of the service type member and
        /// the value is the service type
        /// </summary>
        private static readonly Dictionary<string, ApiServiceType> _lookup =
            Enum.GetValues<ApiServiceType>().ToDictionary(e => e.ToString(), e => e);

        /// <summary>
        /// Declare a map of service type, endpoint type and implementation type
        /// </summary>
        private static readonly Dictionary<(ApiServiceType, ApiEndpointType), Type> _map = new()
        {
            {(ApiServiceType.AeroDataBox, ApiEndpointType.HistoricalFlights), typeof(AeroDataBoxHistoricalFlightApi) },
            {(ApiServiceType.AeroDataBox, ApiEndpointType.Aircraft), typeof(AeroDataBoxAircraftApi) },
            {(ApiServiceType.AirLabs, ApiEndpointType.ActiveFlights), typeof(AirLabsActiveFlightApi) },
            {(ApiServiceType.AirLabs, ApiEndpointType.Airlines), typeof(AirLabsAirlinesApi) },
            {(ApiServiceType.AirLabs, ApiEndpointType.Aircraft), typeof(AirLabsAircraftApi) },
            {(ApiServiceType.CheckWXApi, ApiEndpointType.METAR), typeof(CheckWXMetarApi) },
        };

        /// <summary>
        /// Create and configure an instance of the external API wrapper class using the specified service
        /// and flights API type
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="client"></param>
        /// <param name="context"></param>
        /// <param name="trackedAircraftWriter"></param>
        /// <param name="service"></param>
        /// <param name="flightsEndpointType"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static IExternalApiWrapper GetWrapperInstance(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            BaseStationReaderDbContext context,
            ITrackedAircraftWriter trackedAircraftWriter,
            ApiServiceType service,
            ApiEndpointType flightsEndpointType,
            ExternalApiSettings settings)
        {
            // Create the database management objects
            var airlineManager = new AirlineManager(context);
            var aircraftManager = new AircraftManager(context);
            var manufacturerManager = new ManufacturerManager(context);
            var modelManager = new ModelManager(context);
            var flightManager = new FlightManager(context);
            var sightingManager = new SightingManager(context);

            // Create an instance of the wrapper
            var wrapper = new ExternalApiWrapper(
                settings.MaximumLookups,
                logger,
                airlineManager,
                aircraftManager,
                manufacturerManager,
                modelManager,
                flightManager,
                sightingManager,
                trackedAircraftWriter);

            // Get an instance of the flights API and register it
            var flightsApi = GetApiInstance(
                service,
                flightsEndpointType,
                logger,
                client,
                settings);

            if (flightsApi != null)
            {
                wrapper.RegisterExternalApi(flightsEndpointType, flightsApi);
            }


            // Get an instance of the airlines API and register it
            var airlinesApi = GetApiInstance(
                service,
                ApiEndpointType.Airlines,
                logger,
                client,
                settings);

            if (airlinesApi != null)
            {
                wrapper.RegisterExternalApi(ApiEndpointType.Airlines, airlinesApi);
            }

            // Get an instance of the aircraft API and register it
            var aircraftApi = GetApiInstance(
                service,
                ApiEndpointType.Aircraft,
                logger,
                client,
                settings);

            if (aircraftApi != null)
            {
                wrapper.RegisterExternalApi(ApiEndpointType.Aircraft, aircraftApi);
            }

            // Get an instance of the metar API and register it
            var metarApi = GetApiInstance(
                service,
                ApiEndpointType.METAR,
                logger,
                client,
                settings);

            if (metarApi != null)
            {
                wrapper.RegisterExternalApi(ApiEndpointType.METAR, metarApi);
            }

            return wrapper;
        }

        /// <summary>
        /// Get an instance of an API given the service and endpoint type
        /// </summary>
        /// <param name="service"></param>
        /// <param name="endpoint"></param>
        /// <param name="logger"></param>
        /// <param name="client"></param>
        /// <param name="settings"></param>
        /// <returns></returns>
        public static IExternalApi GetApiInstance(
            ApiServiceType service,
            ApiEndpointType endpoint,
            ITrackerLogger logger,
            ITrackerHttpClient client,
            ExternalApiSettings settings)
        {
            // Get the type for the service
            if (!_map.TryGetValue((service, endpoint), out Type type))
            {
                logger.LogMessage(Severity.Warning, $"{endpoint} API for service {service} is not registered");
                return null;
            }

            // Create an instance of the type
            var instance = Activator.CreateInstance(type, logger, client, settings);
            if (instance == null)
            {
                logger.LogMessage(Severity.Error, $"Failed to create instance of {type.Name}");
                return null;
            }

            // Check the type of the instance is as expected
            if (instance is not IExternalApi typed)
            {
                logger.LogMessage(Severity.Error, $"Created instance is of type {instance.GetType().Name}, expected {typeof(IExternalApi).Name}");
                return null;
            }

            return typed;
        }

        /// <summary>
        /// Return an API service type given a string representation of a service type that may or may
        /// not be valid/supported
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        public static ApiServiceType GetServiceTypeFromString(string type)
            => !string.IsNullOrEmpty(type) && _lookup.ContainsKey(type) ? _lookup[type] : ApiServiceType.None;
    }
}