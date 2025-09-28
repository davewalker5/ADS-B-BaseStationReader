using System.Globalization;
using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Data;
using BaseStationReader.Entities.Interfaces;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Lookup;

namespace BaseStationReader.BusinessLogic.Api
{
    public abstract class ApiWrapperBase
    {
        protected ITrackerLogger _logger;
        protected IAircraftManager _aircraftManager;
        protected IAircraftApi _aircraftApi;
        protected IAirlineManager _airlineManager;
        protected IFlightManager _flightManager;
        protected IModelManager _modelManager;
        protected IManufacturerManager _manufacturerManager;
        protected ISightingManager _sightingManager;

        /// <summary>
        /// Initialise the API wrapper
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="client"></param>
        /// <param name="apiConfiguration"></param>
        public virtual void Initialise(ITrackerLogger logger, ITrackerHttpClient client, BaseStationReaderDbContext context)
        {
            _logger = logger;

            // Construct the database management instances
            _airlineManager = new AirlineManager(context);
            _flightManager = new FlightManager(context);
            _aircraftManager = new AircraftManager(context);
            _modelManager = new ModelManager(context);
            _manufacturerManager = new ManufacturerManager(context);
            _sightingManager = new SightingManager(context);
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
        /// Lookup an active flight and store it
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
#pragma warning disable CS1998
        public virtual async Task<Flight> LookupAndStoreFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Would be better as an abstract method but this isn't permitted with an async function. Classes
            // inheriting from this one must override this method. Having the method here allows the common
            // implementation of "LookupAsync" to be moved into this base class.
            return null;
        }
#pragma warning restore CS1998

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
        /// Parse a string representation of a UTC date and time to give the equivalent local date and time
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        protected static DateTime? ExtractTimestamp(string value)
            => DateTime.TryParse(value, null, DateTimeStyles.AdjustToUniversal, out DateTime utc) ? utc.ToLocalTime() : null;

        /// <summary>
        /// Return an integer value from a property value
        /// </summary>
        /// <param name="property"></param>
        /// <returns></returns>
        protected static int? GetIntegerValue(string property)
            => int.TryParse(property, out int value) ? value : null;

        /// <summary>
        /// Create a flight object from a dictionary of properties returned from the API
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        protected static Flight CreateFlightFromPropertyDictionary(Dictionary<ApiProperty, string> properties)
            => new()
            {
                Embarkation = properties[ApiProperty.EmbarkationIATA],
                Destination = properties[ApiProperty.DestinationIATA],
                IATA = properties[ApiProperty.FlightIATA],
                ICAO = properties[ApiProperty.FlightICAO],
                Number = properties[ApiProperty.FlightNumber],
                Airline = new()
                {
                    IATA = properties[ApiProperty.AirlineIATA],
                    ICAO = properties[ApiProperty.AirlineICAO],
                    Name = properties[ApiProperty.AirlineName]
                },
                AircraftAddress = properties[ApiProperty.AircraftAddress],
                ModelICAO = properties[ApiProperty.ModelICAO]
            };
    }
}