using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using BaseStationReader.Logic.Api;
using BaseStationReader.Logic.Api.AirLabs;
using BaseStationReader.Logic.Database;
using BaseStationReader.Logic.Tracking;
using System;
using System.Threading.Tasks;

namespace BaseStationReader.UI.Models
{
    public class AircraftLookupModel
    {
        private readonly AircraftLookupManager _lookupManager;

        public AircraftLookupModel(ITrackerLogger logger, TrackerApplicationSettings settings)
        {
            // Create a database context
            var context = new BaseStationReaderDbContextFactory().CreateDbContext(Array.Empty<string>());

            // Create the database management instances
            var airlinesManager = new AirlineManager(context);
            var detailsManager = new AircraftDetailsManager(context);
            var modelsManager = new ModelManager(context);

            // Get the service endpoint details
            var key = settings.ApiServiceKeys.Find(x => x.Service == ApiServiceType.AirLabs)!.Key;
            var airlinesUrl = settings.ApiEndpoints.Find(x => x.EndpointType == ApiEndpointType.Airlines)!.Url;
            var aircraftUrl = settings.ApiEndpoints.Find(x => x.EndpointType == ApiEndpointType.Aircraft)!.Url;
            var flightsUrl = settings.ApiEndpoints.Find(x => x.EndpointType == ApiEndpointType.ActiveFlights)!.Url;

            // Create the API wrappers
            var client = TrackerHttpClient.Instance;
            var airlinesApi = new AirLabsAirlinesApi(logger, client, airlinesUrl, key);
            var aircraftApi = new AirLabsAircraftApi(logger, client, aircraftUrl, key);
            var flightsApi = new AirLabsActiveFlightApi(logger, client, flightsUrl, key);

            // Finally, create a lookup manager
            _lookupManager = new AircraftLookupManager(airlinesManager, detailsManager, modelsManager, airlinesApi, aircraftApi, flightsApi);
        }

        /// <summary>
        /// Look up the details of the specified aircraft
        /// </summary>
        /// <param name="address"></param>
        public AircraftDetails? LookupAircraft(string? address)
        {
            AircraftDetails? details = null;

            if (!string.IsNullOrEmpty(address))
            {
                details = Task.Run(() => _lookupManager.LookupAircraft(address)).Result;
            }

            return details;
        }

        /// <summary>
        /// Look for active flights for the aircraft with the specified ICAO address
        /// </summary>
        /// <param name="address"></param>
        public FlightDetails? LookupActiveFlight(string? address)
        {
            FlightDetails? details = null;

            if (!string.IsNullOrEmpty(address))
            {
                details = Task.Run(() => _lookupManager.LookupActiveFlight(address)).Result;
            }

            return details;
        }
    }
}
