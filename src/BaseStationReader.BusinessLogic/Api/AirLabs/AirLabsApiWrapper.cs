using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.BusinessLogic.Api.AirLabs
{
    public class AirLabsApiWrapper : IApiWrapper
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

        public AirLabsApiWrapper(
            ITrackerLogger logger,
            ITrackerHttpClient client,
            BaseStationReaderDbContext context,
            string airlinesEndpointUrl,
            string aircraftEndpointUrl,
            string flightsEndpointUrl,
            string key)
        {
            _logger = logger;

            // Construct the database management instances
            _airlineManager = new AirlineManager(context);
            _flightManager = new FlightManager(context);
            _aircraftManager = new AircraftManager(context);
            _modelManager = new ModelManager(context);
            _manufacturerManager = new ManufacturerManager(context);

            // Construct the API instances
            _airlinesApi = new AirLabsAirlinesApi(logger, client, airlinesEndpointUrl, key);
            _aircraftApi = new AirLabsAircraftApi(logger, client, aircraftEndpointUrl, key);
            _flightsApi = new AirLabsActiveFlightApi(logger, client, flightsEndpointUrl, key);
        }

        /// <summary>
        /// Lookup an active flight using the aircraft's ICAO 24-bit ICAO address
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Flight> LookupFlightAsync(string address)
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
        /// Lookup an active flight and store it
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Flight> LookupAndStoreFlightAsync(string address)
        {
            // Request flight details for an active flight involving the aircraft with the specified ICAO address
            var flight = await LookupFlightAsync(address);
            if (flight != null)
            {
                // Get the airline details, storing them locally if not already present
                var airline = await LookupAndStoreAirlineAsync(flight.Airline.ICAO, flight.Airline.IATA);
                if (airline != null)
                {
                    // Airline details have been retrieved OK so create the flight (the flight manager prevents creation
                    // of duplicates)
                    flight = await _flightManager.AddAsync(flight.IATA, flight.ICAO, flight.Number, flight.Embarkation, flight.Destination, airline.Id);
                }
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
                _logger.LogMessage(Severity.Debug, $"Airline {icao} ({iata}) is not stored locally : Using the API");

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
                else
                {
                    _logger.LogMessage(Severity.Debug, $"API lookup for airline {icao} ({iata}) produced no results");
                }
            }

            return airline;
        }

        /// <summary>
        /// Retrieve or lookup an airline, making sure it's saved locally
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <returns></returns>
        public async Task<Airline> LookupAndStoreAirlineAsync(string icao, string iata)
        {
            // Attempt to load the airline based on its ICAO or IATA code
            var airline = await LookupAirlineAsync(icao, iata);
            if ((airline != null) && (airline.Id == 0))
            {
                // Airline was found but was not loaded from the database, so save it
                airline = await _airlineManager.AddAsync(airline.IATA, airline.ICAO, airline.Name);
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
                _logger.LogMessage(Severity.Debug, $"Aircraft {address} is not stored locally : Using the API");

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
                else
                {
                    _logger.LogMessage(Severity.Debug, $"API lookup for aircraft {address} produced no results");
                }
            }

            return aircraft;
        }

        /// <summary>
        /// Retrieve or lookup an aircraft, making sure it's saved locally
        /// </summary>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<Aircraft> LookupAndStoreAircraftAsync(string address)
        {
            // Attempt to load the aircraft based on its 24-bit ICAO address
            var aircraft = await LookupAircraftAsync(address);
            if ((aircraft != null) && (aircraft.Id == 0))
            {
                // Save the manufacturer and model - the management classes prevent creation of duplicates
                var manufacturer = await _manufacturerManager.AddAsync(aircraft.Model.Manufacturer.Name);
                var model = await _modelManager.AddAsync(
                    aircraft.Model.IATA, aircraft.Model.ICAO, aircraft.Model.Name, manufacturer.Id);

                // Save the aircraft
                aircraft = await _aircraftManager.AddAsync(
                    aircraft.Address, aircraft.Registration, aircraft.Manufactured, aircraft.Age, model.Id);
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
