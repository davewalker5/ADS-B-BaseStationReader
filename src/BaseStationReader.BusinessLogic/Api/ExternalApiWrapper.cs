using System.Collections.Concurrent;
using System.Globalization;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Api
{
    public class ExternalApiWrapper : IExternalApiWrapper
    {
        private readonly ConcurrentDictionary<ApiEndpointType, IExternalApi> _apis = new();

        private readonly ITrackerLogger _logger;
        private readonly IAirlineManager _airlineManager;
        private readonly IAircraftManager _aircraftManager;
        private readonly IManufacturerManager _manufacturerManager;
        private readonly IModelManager _modelManager;
        private readonly IFlightManager _flightManager;
        private readonly ISightingManager _sightingManager;
        private readonly ITrackedAircraftWriter _trackedAircraftWriter;

        public ExternalApiWrapper(
            ITrackerLogger logger,
            IAirlineManager airlineManager,
            IAircraftManager aircraftManager,
            IManufacturerManager manufacturerManager,
            IModelManager modelManager,
            IFlightManager flightManager,
            ISightingManager sightingManager,
            ITrackedAircraftWriter trackedAircraftWriter)
        {
            _logger = logger;
            _airlineManager = airlineManager;
            _aircraftManager = aircraftManager;
            _manufacturerManager = manufacturerManager;
            _modelManager = modelManager;
            _flightManager = flightManager;
            _sightingManager = sightingManager;
            _trackedAircraftWriter = trackedAircraftWriter;
        }

        /// <summary>
        /// Register an external API instance
        /// </summary>
        /// <param name="type"></param>
        /// <param name="api"></param>
        public void RegisterExternalApi(ApiEndpointType type, IExternalApi api)
            => _apis[type] = api;

        /// <summary>
        /// Lookup a flight and aircraft given a 24-bit aircraft ICAO address and filtering parameters
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirports"></param>
        /// <param name="arrivalAirports"></param>
        /// <returns></returns>
        public async Task<LookupResult> LookupAsync(
            ApiEndpointType type,
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes,
            bool createSighting)
        {
            Aircraft aircraft = null;
            Sighting sighting = null;

            // Lookup the flight
            Flight flight = type == ApiEndpointType.ActiveFlights ?
                await LookupActiveFlightAsync(address, departureAirportCodes, arrivalAirportCodes) :
                await LookupHistoricalFlightAsync(address, departureAirportCodes, arrivalAirportCodes);

            if (flight != null)
            {
                // Lookup the aircraft
                aircraft = await LookupAircraftAsync(address, flight.ModelICAO);
                if (createSighting && (aircraft != null))
                {
                    // Save the relationship between the flight and the aircraft as a sighting on this date
                    sighting = await _sightingManager.AddAsync(aircraft.Id, flight.Id, DateTime.Today);
                }
            }

            return new()
            {
                FlightId = flight?.Id,
                AircraftId = aircraft?.Id,
                SightingId = sighting?.Id,
                CreateSighting = createSighting
            };
        }

        /// <summary>
        /// Look up an active flight and store it locally
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirports"></param>
        /// <param name="arrivalAirports"></param>
        /// <returns></returns>
        public async Task<Flight> LookupActiveFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Get the API instance
            if (GetInstance(ApiEndpointType.ActiveFlights) is not IActiveFlightsApi api) return null;

            // The aircraft address must be specified
            if (string.IsNullOrEmpty(address))
            {
                _logger.LogMessage(Severity.Warning, $"Unable to look up flight details : Invalid aircraft address");
                return null;
            }

            // Use the API to look-up the flight
            Flight flight = null;
            var properties = await api.LookupFlightByAircraftAsync(address);
            if (properties?.Count > 0)
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
                    flight = await SaveFlight(properties);
                }
                else
                {
                    _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {address} excluded by the airport filters");
                }
            }

            return flight;
        }

        /// <summary>
        /// Identify and save historical flight details for a tracked aircraft
        /// </summary>
        /// <param name="address"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        public async Task<Flight> LookupHistoricalFlightAsync(
            string address,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Get the API instance
            if (GetInstance(ApiEndpointType.ActiveFlights) is not IHistoricalFlightsApi api) return null;

            // The aircraft address must be specified
            if (string.IsNullOrEmpty(address))
            {
                _logger.LogMessage(Severity.Warning, $"Unable to look up flight details : Invalid aircraft address");
                return null;
            }

            // Retrieve the tracked aircraft record 
            var aircraft = await _trackedAircraftWriter.GetAsync(x => x.Address == address);
            if (aircraft == null)
            {
                _logger.LogMessage(Severity.Warning, $"Unable to look up flight details : Aircraft with address '{address}' is not being tracked");
                return null;
            }

            _logger.LogMessage(Severity.Info, $"Looking up historical flights for aircraft with address '{address}'");

            // Use the API to look-up details for historical flights by the aircraft
            Flight flight = null;
            var properties = await api.LookupFlightsByAircraftAsync(address);
            if (properties?.Count > 0)
            {
                foreach (var flightProperties in properties)
                {
                    // See if this one matches the filtering criteria
                    var matches = FilterFlight(aircraft, flightProperties, departureAirportCodes, arrivalAirportCodes);
                    if (matches)
                    {
                        // Save the airline and the flight
                        var airline = await _airlineManager.AddAsync(
                            flightProperties[ApiProperty.AirlineIATA], flightProperties[ApiProperty.AirlineICAO], flightProperties[ApiProperty.AirlineName]);

                        flight = await SaveFlight(flightProperties);

                        // And as we now have a matching flight, return it
                        return flight;
                    }
                }
            }

            // If we fall through to here, no matching flight's been found
            return null;
        }

        /// <summary>
        /// Lookup all active flights within a bounding box around a central point
        /// </summary>
        /// <param name="centreLatitude"></param>
        /// <param name="centreLongitude"></param>
        /// <param name="rangeNm"></param>
        /// <returns></returns>
        public async Task<List<Flight>> LookupActiveFlightsInBoundingBox(
            double centreLatitude,
            double centreLongitude,
            double rangeNm)
        {
            // Get the API instance
            if (GetInstance(ApiEndpointType.ActiveFlights) is not IActiveFlightsApi api) return null;

            List<Flight> flights = [];

            // Use the API to look-up the flights
            var properties = await api.LookupFlightsInBoundingBox(centreLatitude, centreLongitude, rangeNm);
            if (properties?.Count > 0)
            {
                // Iterate over the collection of flight properties
                foreach (var flightDetails in properties)
                {
                    // Create a flight object from this set of properties and add it to the collection
                    var flight = CreateFlightFromProperties(flightDetails);
                    flights.Add(flight);
                }
            }

            return flights;
        }

        /// <summary>
        /// Look up an airline and save it locally
        /// </summary>
        /// <param name="icao"></param>
        /// <param name="iata"></param>
        /// <returns></returns>
        public async Task<Airline> LookupAirlineAsync(string icao, string iata)
        {
            // Get the API instance
            if (GetInstance(ApiEndpointType.Airlines) is not IAirlinesApi api) return null;

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
                    await api.LookupAirlineByICAOCodeAsync(icao) :
                    await api.LookupAirlineByICAOCodeAsync(iata);

                if (properties?.Count > 0)
                {
                    // Create a new airline object containing the details returned by the API
                    airline = await _airlineManager.AddAsync(
                        properties[ApiProperty.AirlineIATA],
                        properties[ApiProperty.AirlineICAO],
                        properties[ApiProperty.AirlineName]);
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
        /// Look up an aircraft and save it locally
        /// </summary>
        /// <param name="address"></param>
        /// <param name="alternateModelICAO"></param>
        /// <returns></returns>
        public async Task<Aircraft> LookupAircraftAsync(string address, string alternateModelICAO)
        {
            // Get the API instance
            if (GetInstance(ApiEndpointType.Aircraft) is not IAircraftApi api) return null;

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
                var properties = await api.LookupAircraftAsync(address);
                if (properties?.Count > 0)
                {
                    // If the aircraft is returned without a model and we have and alternative ICAO for the
                    // model (often from the flight), then use that
                    var modelICAO = string.IsNullOrEmpty(properties[ApiProperty.ModelICAO]) ?
                        alternateModelICAO ?? "" :
                        properties[ApiProperty.ModelICAO];

                    // Get the year of manufacture of the aircraft and determine its age
                    var manufactured = GetYearOfManufacture(properties[ApiProperty.AircraftManufactured]);
                    int? age = manufactured != null ? DateTime.Today.Year - manufactured : null;

                    // Save the manufacturer, model and aircraft
                    var manufacturer = await _manufacturerManager.AddAsync(properties[ApiProperty.ManufacturerName]);
                    var model = await _modelManager.AddAsync(
                        properties[ApiProperty.ModelIATA], modelICAO, properties[ApiProperty.ModelName], manufacturer.Id);
                    aircraft = await _aircraftManager.AddAsync(
                        address, properties[ApiProperty.AircraftRegistration], manufactured, age, model.Id);
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
        /// Lookup the current weather for an airport
        /// </summary>
        /// <param name="icao"></param>
        /// <returns></returns>
        public async Task<IEnumerable<string>> LookupAirportWeather(string icao)
        {
            // Get the API instance
            if (GetInstance(ApiEndpointType.METAR) is not IMetarApi api) return null;

            // Lookup the weather for the requested airport
            var results = await api.LookupAirportWeather(icao);
            return results;
        }

        /// <summary>
        /// Create a flight object from a dictionary of properties returned from the API
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private static Flight CreateFlightFromProperties(Dictionary<ApiProperty, string> properties)
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

        /// <summary>
        /// Given a set of API property values representing a flight, create and save a new flight
        /// locally
        /// </summary>
        /// <param name="properties"></param>
        /// <returns></returns>
        private async Task<Flight> SaveFlight(Dictionary<ApiProperty, string> properties)
        {
            // Save the airline and the flight
            var airline = await _airlineManager.AddAsync(
                properties[ApiProperty.AirlineIATA], properties[ApiProperty.AirlineICAO], properties[ApiProperty.AirlineName]);

            Flight flight = await _flightManager.AddAsync(
                properties[ApiProperty.FlightIATA],
                properties[ApiProperty.FlightICAO],
                properties[ApiProperty.FlightNumber],
                properties[ApiProperty.EmbarkationIATA],
                properties[ApiProperty.DestinationIATA],
                airline.Id);

            // There may be additional aircraft details in the flight properties
            flight.AircraftAddress = properties[ApiProperty.AircraftAddress];
            flight.ModelICAO = properties[ApiProperty.ModelICAO];

            // And as we now have a matching flight, return it
            return flight;
        }

        /// <summary>
        /// Determine if a property collection representing a flight matches a set of filtering criteria
        /// </summary>
        /// <param name="aircraft"></param>
        /// <param name="properties"></param>
        /// <param name="departureAirportCodes"></param>
        /// <param name="arrivalAirportCodes"></param>
        /// <returns></returns>
        private bool FilterFlight(
            TrackedAircraft aircraft,
            Dictionary<ApiProperty, string> properties,
            IEnumerable<string> departureAirportCodes,
            IEnumerable<string> arrivalAirportCodes)
        {
            // Extract the departure and arrival airport codes
            var departure = properties[ApiProperty.EmbarkationIATA];
            var arrival = properties[ApiProperty.DestinationIATA];

            _logger.LogMessage(Severity.Info, $"Checking flight with route {departure} - {arrival} against the filters for aircraft {aircraft.Address}");

            // Extract the address and check it matches
            var address = properties[ApiProperty.AircraftAddress];
            if (address != aircraft.Address)
            {
                _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {address} does not have the correct aircraft address");
                return false;
            }

            // Check the airport codes against the filters
            var departureAllowed = !(departureAirportCodes?.Count() > 0) || departureAirportCodes.Contains(departure);
            var arrivalAllowed = !(arrivalAirportCodes?.Count() > 0) || arrivalAirportCodes.Contains(arrival);

            if (!departureAllowed || !arrivalAllowed)
            {
                _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {aircraft.Address} excluded by the airport filters");
                return false;
            }

            // Extract the departure and arrival times
            var departureTime = ExtractTimestamp(properties[ApiProperty.DepartureTime]);
            var arrivalTime = ExtractTimestamp(properties[ApiProperty.ArrivalTime]);

            // Check the flight times include the last seen date on the aircraft
            if (!departureTime.HasValue || (departureTime.Value > aircraft.LastSeen) ||
                !arrivalTime.HasValue || (arrivalTime.Value < aircraft.LastSeen))
            {
                _logger.LogMessage(Severity.Info, $"Route {departure} - {arrival} for aircraft {aircraft.Address} excluded by the flight time filters");
                return false;
            }

            _logger.LogMessage(Severity.Info, $"Flight with route {departure} - {arrival} matches filters for aircraft {aircraft.Address}");
            return true;
        }

        /// <summary>
        /// Parse a string representation of a UTC date and time to give the equivalent local date and time
        /// </summary>
        /// <param name="value"></param>
        /// <returns></returns>
        private static DateTime? ExtractTimestamp(string value)
            => DateTime.TryParse(value, null, DateTimeStyles.AdjustToUniversal, out DateTime utc) ? utc.ToLocalTime() : null;

        /// <summary>
        /// Extract the year of manufacture from a string representation of either the integer year or
        /// a date
        /// </summary>
        /// <param name="manufactured"></param>
        /// <returns></returns>
        private static int? GetYearOfManufacture(string manufactured)
        {
            if (!string.IsNullOrEmpty(manufactured))
            {
                if (int.TryParse(manufactured, out int year))
                {
                    return year;
                }

                if (DateTime.TryParseExact(manufactured, "yyyy-MM-dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime dateOfManufacture))
                {
                    year = dateOfManufacture.Year;
                }
            }

            return null;
        }

        /// <summary>
        /// Retrieve an API instance from the collection
        /// </summary>
        /// <param name="type"></param>
        /// <returns></returns>
        private IExternalApi GetInstance(ApiEndpointType type)
        {
            if (_apis.TryGetValue(type, out var api))
            {
                _logger.LogMessage(Severity.Debug, $"{type} API is of type {api.GetType().Name}");
            }
            else
            {
                _logger.LogMessage(Severity.Error, $"{type} API not registered");
            }

            return api;
        }
    }
}