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
        private readonly ISightingManager _sightingManager;
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
            _sightingManager = new SightingManager(context);

            // Construct the API instances
            _airlinesApi = new AirLabsAirlinesApi(logger, client, airlinesEndpointUrl, key);
            _aircraftApi = new AirLabsAircraftApi(logger, client, aircraftEndpointUrl, key);
            _flightsApi = new AirLabsActiveFlightApi(logger, client, flightsEndpointUrl, key);
        }

        /// <summary>
        /// Lookup a flight and aircraft given a 24-bit aircraft ICAO address and filtering parameters
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirports"></param>
        /// <param name="arrivalAirports"></param>
        /// <returns></returns>
        public async Task LookupAsync(
            string address,
            IEnumerable<string> departureAirports,
            IEnumerable<string> arrivalAirports,
            bool createSighting)
        {
            // Lookup the flight
            var flight = await LookupAndStoreFlightAsync(address, departureAirports, arrivalAirports);
            if (flight != null)
            {
                // Lookup the aircraft, but only if the flight was found/returned. The flight
                // could be filtered out, in which case we don't want to store any of the details.
                // Note, also, that the flight may contain the aircraft model that can be used to
                // fill in model and manufacturer details if the aircraft request doesn't include them
                var aircraft = await LookupAndStoreAircraftAsync(address, flight.ModelICAO);
                if (createSighting && (aircraft != null))
                {
                    // Save the relationship between the flight and the aircraft as a sighting on this date
                    _ = await _sightingManager.AddAsync(aircraft.Id, flight.Id, DateTime.Today);
                }
            }
        }

        /// <summary>
        /// Lookup an active flight using the aircraft's ICAO 24-bit ICAO address
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
                // Extract the departure and arrival airport codes
                var departure = properties[ApiProperty.EmbarkationIATA];
                var arrival = properties[ApiProperty.DestinationIATA];

                _logger.LogMessage(Severity.Info, $"Found flight with route {departure} - {arrival} for aircraft {address}");

                // Check the codes against the filters
                var departureAllowed = departureAirportCodes?.Count() > 0 ? departureAirportCodes.Contains(departure) : true;
                var arrivalAllowed = arrivalAirportCodes?.Count() > 0 ? arrivalAirportCodes.Contains(arrival) : true;

                // Check both airports are found in the "allowed" lists
                if (departureAllowed && arrivalAllowed)
                {
                    // Create a new flight object containing the details returned by the API
                    flight = new()
                    {
                        Embarkation = departure,
                        Destination = arrival,
                        IATA = properties[ApiProperty.FlightIATA],
                        ICAO = properties[ApiProperty.FlightICAO],
                        Number = properties[ApiProperty.FlightNumber],
                        Airline = new()
                        {
                            IATA = properties[ApiProperty.AirlineIATA],
                            ICAO = properties[ApiProperty.AirlineICAO]
                        },
                        ModelICAO = properties[ApiProperty.ModelICAO]
                    };
                }
                else
                {
                    _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {address} excluded by the airport filters");
                }
            }

            return flight;
        }

        /// <summary>
        /// Lookup an active flight and store it
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        public async Task<Flight> LookupAndStoreFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Request flight details for an active flight involving the aircraft with the specified ICAO address
            var flight = await LookupFlightAsync(address, departureAirportCodes, arrivalAirportCodes);
            if (flight != null)
            {
                // Get the airline details, storing them locally if not already present
                var airline = await LookupAndStoreAirlineAsync(flight.Airline.ICAO, flight.Airline.IATA);
                if (airline != null)
                {
                    // Capture the alternative ICAO for the aircraft model before saving as it's not a persisted
                    // property and will need to be restored afterwards
                    var alternateModelICAO = flight.ModelICAO;

                    // Airline details have been retrieved OK so create the flight (the flight manager prevents creation
                    // of duplicates)
                    flight = await _flightManager.AddAsync(flight.IATA, flight.ICAO, flight.Number, flight.Embarkation, flight.Destination, airline.Id);
                    flight.ModelICAO = alternateModelICAO;
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
            var airline = await _airlineManager.GetByCodeAsync(iata, icao);
            if (airline == null)
            {
                _logger.LogMessage(Severity.Info, $"Airline with ICAO = '{icao}', IATA = '{iata}' is not stored locally : Using the API");

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
                    _logger.LogMessage(Severity.Info, $"API lookup for Airline with ICAO = '{icao}', IATA = '{iata}' produced no results");
                }
            }
            else
            {
                _logger.LogMessage(Severity.Info, $"Airline with ICAO = '{icao}', IATA = '{iata}' retrieved from the database");
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
        /// <param name="alternateModelICAO"></param>
        /// <returns></returns>
        public async Task<Aircraft> LookupAircraftAsync(string address, string alternateModelICAO)
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
                _logger.LogMessage(Severity.Info, $"Aircraft {address} is not stored locally : Using the API");

                // Not stored locally, so use the API to look it up
                var properties = await _aircraftApi.LookupAircraftAsync(address);
                if (properties != null)
                {
                    // If the aircraft is returned without a model and we have and alternative ICAO for the
                    // model (often from the flight), then use that
                    var modelICAO = string.IsNullOrEmpty(properties[ApiProperty.ModelICAO]) ?
                        alternateModelICAO ?? "" :
                        properties[ApiProperty.ModelICAO];

                    aircraft = new()
                    {
                        Address = address,
                        Registration = properties[ApiProperty.AircraftRegistration],
                        Manufactured = GetIntegerValue(properties[ApiProperty.AircraftManufactured]),
                        Age = GetIntegerValue(properties[ApiProperty.AircraftAge]),
                        Model = new()
                        {
                            ICAO = modelICAO,
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
                    _logger.LogMessage(Severity.Info, $"API lookup for aircraft {address} produced no results");
                }
            }
            else
            {
                _logger.LogMessage(Severity.Info, $"Aircraft {address} retrieved from the database");
            }

            return aircraft;
        }

        /// <summary>
        /// Retrieve or lookup an aircraft, making sure it's saved locally
        /// </summary>
        /// <param name="address"></param>
        /// <param name="alternateModelICAO"></param>
        /// <returns></returns>
        public async Task<Aircraft> LookupAndStoreAircraftAsync(string address, string alternateModelICAO)
        {
            // Attempt to load the aircraft based on its 24-bit ICAO address
            var aircraft = await LookupAircraftAsync(address, alternateModelICAO);
            if ((aircraft != null) && (aircraft.Id == 0))
            {
                // See if the model is already in the database
                var model = await _modelManager.GetByCodeAsync(aircraft.Model.IATA, aircraft.Model.ICAO);
                if (model == null)
                {
                    // Save the manufacturer and model
                    _logger.LogMessage(Severity.Debug,
                        $"Model '{aircraft.Model.Name}', ICAO = '{aircraft.Model.ICAO}', IATA = '{aircraft.Model.IATA}', manufacturer = '{aircraft.Model.Manufacturer.Name}' is not stored locally");

                    var manufacturer = await _manufacturerManager.AddAsync(aircraft.Model.Manufacturer.Name);
                    model = await _modelManager.AddAsync(aircraft.Model.IATA, aircraft.Model.ICAO, aircraft.Model.Name, manufacturer.Id);
                }
                else
                {
                    _logger.LogMessage(Severity.Debug,
                        $"Model '{model.Name}', ICAO = '{model.ICAO}', IATA = '{model.IATA}' was retrieved from the database");
                }

                // Save the aircraft
                aircraft = await _aircraftManager.AddAsync(aircraft.Address, aircraft.Registration, aircraft.Manufactured, aircraft.Age, model.Id);
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
