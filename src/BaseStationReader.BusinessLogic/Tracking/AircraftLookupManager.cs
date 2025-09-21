using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.BusinessLogic.Tracking
{
    public class AircraftLookupManager : IAircraftLookupManager
    {
        private readonly ITrackerLogger _logger;
        private readonly IAirlineManager _airlineManager;
        private readonly IFlightManager _flightManager;
        private readonly IAircraftManager _aircraftManager;
        private readonly IModelManager _modelManager;
        private readonly IManufacturerManager _manufacturerManager;
        private readonly IAirlinesApi _airlinesApi;
        private readonly IAircraftApi _aircraftApi;
        private readonly IActiveFlightApi _flightsApi;

        public AircraftLookupManager(
            ITrackerLogger logger,
            IAirlineManager airlineManager,
            IFlightManager flightManager,
            IAircraftManager detailsManager,
            IModelManager modelManager,
            IManufacturerManager manufacturerManager,
            IAirlinesApi airlinesApi,
            IAircraftApi aircraftApi,
            IActiveFlightApi flightsApi)
        {
            _logger = logger;
            _airlineManager = airlineManager;
            _flightManager = flightManager;
            _aircraftManager = detailsManager;
            _modelManager = modelManager;
            _manufacturerManager = manufacturerManager;
            _airlinesApi = airlinesApi;
            _aircraftApi = aircraftApi;
            _flightsApi = flightsApi;
        }

        /// <summary>
        /// Lookup an active flight using the aircraft's ICAO 24-bit ICAO address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Flight> LookupActiveFlightAsync(string address)
        {
            Flight flight = null;

            // The aircraft address must be specified
            if (string.IsNullOrEmpty(address))
            {
                _logger.LogMessage(Severity.Warning, $"Unable to look up flight details : Invalid aircraft address");
                return null;
            }

            // Use the API to look-up the flight
            var properties = await _flightsApi.LookupFlightByAircraftAsync(address);
            if (properties != null)
            {
                // Create a new flight object containing the details returned by the API
                flight = new()
                {
                    Embarkation = properties[ApiProperty.EmbarkationIATA],
                    Destination = properties[ApiProperty.DestinationIATA],
                    IATA = properties[ApiProperty.FlightIATA],
                    ICAO = properties[ApiProperty.FlightICAO],
                    Number = properties[ApiProperty.FlightNumber],
                    Airline = new()
                    {
                        IATA = properties[ApiProperty.AirlineIATA],
                        ICAO = properties[ApiProperty.AirlineICAO]
                    }
                };
            }

            return flight;
        }

        /// <summary>
        /// Retrieve or look up and aircraft given it's ICAO and/or IATA code
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <returns></returns>
        public async Task<Airline> LookupAirlineAsync(string icao, string iata)
        {
            // At least one of the parameters must be specified
            if (string.IsNullOrEmpty(icao) && string.IsNullOrEmpty(iata))
            {
                _logger.LogMessage(Severity.Warning, $"Unable to look up airline details : Invalid ICAO and IATA codes");
                return null;
            }

            // See if the airline is stored locally, first
            Airline airline = !string.IsNullOrEmpty(icao) ?
                await _airlineManager.GetAsync(x => x.ICAO == icao) :
                await _airlineManager.GetAsync(x => x.IATA == iata);

            if (airline == null)
            {
                // Not stored locally, so use the API to look it up
                var properties = !string.IsNullOrEmpty(icao) ?
                    await _airlinesApi.LookupAirlineByICAOCodeAsync(icao) :
                    await _airlinesApi.LookupAirlineByICAOCodeAsync(iata);

                if (properties != null)
                {
                    // Create a new airline object containing the details returned by the API
                    airline = new()
                    {
                        IATA = properties[ApiProperty.AirlineIATA],
                        ICAO = properties[ApiProperty.AirlineICAO],
                        Name = properties[ApiProperty.AirlineName]
                    };
                }
            }

            return airline;
        }

        /// <summary>
        /// Lookup an aircraft's details given its ICAO 24-bit address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Aircraft> LookupAircraftAsync(string address)
        {
            // The aircraft address must be specified
            if (string.IsNullOrEmpty(address))
            {
                _logger.LogMessage(Severity.Warning, $"Unable to look up aircraft details : Invalid aircraft address");
                return null;
            }

            // See if the aircraft is stored locally, first
            var aircraft = await _aircraftManager.GetAsync(x => x.Address == address);
            if (aircraft == null)
            {
                // Not stored locally, so use the API to look it up
                var properties = await _aircraftApi.LookupAircraftAsync(address);
                if (properties != null)
                {
                    aircraft = new()
                    {
                        Address = address,
                        Registration = properties[ApiProperty.AircraftRegistration],
                        Manufactured = GetIntegerValue(properties[ApiProperty.AircraftManufactured]),
                        Age = GetIntegerValue(properties[ApiProperty.AircraftAge]),
                        Model = new()
                        {
                            ICAO = properties[ApiProperty.ModelICAO],
                            IATA = properties[ApiProperty.ModelIATA],
                            Name = properties[ApiProperty.ModelName],
                            Manufacturer = new()
                            {
                                Name = properties[ApiProperty.ManufacturerName]
                            }
                        }
                    };
                }

            }

            return aircraft;
        }

        /// <summary>
        /// Return an integer value from a property value
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        private static int? GetIntegerValue(string property)
            => int.TryParse(property, out int value) ? value : null;
    }
}
