using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Lookup;
using BaseStationReader.Entities.Tracking;
using System.Diagnostics.CodeAnalysis;

namespace BaseStationReader.Logic.Tracking
{
    [ExcludeFromCodeCoverage]
    public class AircraftLookupManager
    {
        private readonly IAirlineManager _airlineManager;
        private readonly IAircraftDetailsManager _detailsManager;
        private readonly IModelManager _modelManager;
        private readonly IAirlinesApi _airlinesApi;
        private readonly IAircraftApi _aircraftApi;

        public AircraftLookupManager(
            IAirlineManager airlineManager,
            IAircraftDetailsManager detailsManager,
            IModelManager modelManager,
            IAirlinesApi airlinesApi,
            IAircraftApi aircraftApi)
        {
            _airlineManager = airlineManager;
            _detailsManager = detailsManager;
            _modelManager = modelManager;
            _airlinesApi = airlinesApi;
            _aircraftApi = aircraftApi;
        }

        /// <summary>
        /// Lookup an aircraft's details given its ICAO 24-bit address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<AircraftDetails?> LookupAircraft(string address)
        {
            // See if the details are locally cached, first
            var details = await _detailsManager!.GetAsync(x => x.Address == address);
            if (details == null)
            {
                // Not locally cached, so request a set of properties via the aircraft API
                var properties = await _aircraftApi!.LookupAircraft(address);
                if (properties != null)
                {
                    // Retrieve the model
                    var model = await GetModel(properties[ApiProperty.ModelIATA], properties[ApiProperty.ModelICAO]);

                    // If we don't have model details, there's no point caching the aircraft details
                    // locally, so check we have a model
                    if (model != null)
                    {
                        // Get the airline details
                        var airline = await GetAirlineFromResponse(properties[ApiProperty.AirlineIATA], properties[ApiProperty.AirlineICAO]);

                        // Add a new aircraft details record to the local database
                        details = await _detailsManager.AddAsync(address, airline?.Id, model.Id);
                    }
                }

            }

            return details;
        }

        /// <summary>
        /// Retrieve the model given the IATA and ICAO codes
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <returns></returns>
        private async Task<Model?> GetModel(string iata, string icao)
        {
            // Look for a match for both the IATA and ICAO codes
            Model? model = await _modelManager!.GetAsync(x => (x.IATA == iata) && (x.ICAO == icao));

            // See if there's a match? If not, use the IATA code alone. This provides more granularity
            // than the ICAO code alone. For example, there are multiple aircraft models with ICAO
            // designation B738, but each has a different IATA code
            if ((model == null) && !string.IsNullOrEmpty(iata))
            {
                model = await _modelManager!.GetAsync(x => x.IATA == iata);
            }

            // See if there's a match? If not, fallback to using the ICAO code alone
            if ((model == null) && !string.IsNullOrEmpty(icao))
            {
                model = await _modelManager!.GetAsync(x => x.ICAO == icao);
            }

            return model;
        }

        /// <summary>
        /// Get an airline instance with the properties returned by the API
        /// </summary>
        /// <param name="iata"></param>
        /// <param name="icao"></param>
        /// <returns></returns>
        private async Task<Airline?> GetAirlineFromResponse(string iata, string icao)
        {
            // See if the airline has been cached locally
            Airline? airline = await _airlineManager!.GetAsync(x => (x.IATA == iata) || (x.ICAO == icao));
            if (airline == null)
            {
                // Not cached locally, so look the airline up using the API, either using the ICAO code or IATA
                // code, whichever is valid
                Dictionary<ApiProperty, string>? properties = null;
                if (!string.IsNullOrEmpty(icao))
                {
                    properties = await _airlinesApi!.LookupAirlineByICAOCode(icao);
                }
                else if (!string.IsNullOrEmpty(iata))
                {
                    properties = await _airlinesApi!.LookupAirlineByIATACode(iata);
                }

                // Check we have some airline properties
                if (properties != null)
                {
                    // Lookup has worked, so cache the airline in the local database
                    airline = await _airlineManager.AddAsync(
                        properties[ApiProperty.AirlineIATA],
                        properties[ApiProperty.AirlineICAO],
                        properties[ApiProperty.AirlineName]);
                }
            }

            return airline;
        }
    }
}
