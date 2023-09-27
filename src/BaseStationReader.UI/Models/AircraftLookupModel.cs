﻿using BaseStationReader.Data;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Lookup;
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

        public AircraftLookupModel(TrackerApplicationSettings settings)
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

            // Create the API wrappers
            var airlinesApi = new AirLabsAirlinesApi(airlinesUrl, key);
            var aircraftApi = new AirLabsAircraftApi(aircraftUrl, key);

            // Finally, create a lookup manager
            _lookupManager = new AircraftLookupManager(airlinesManager, detailsManager, modelsManager, airlinesApi, aircraftApi);
        }

        /// <summary>
        /// Look up the details of the specified aircraft
        /// </summary>
        /// <param name="address"></param>
        public AircraftDetails? Search(string? address)
        {
            AircraftDetails? details = null;

            if (!string.IsNullOrEmpty(address))
            {
                details = Task.Run(() => _lookupManager.LookupAircraft(address)).Result;
            }

            return details;
        }
    }
}
