using System.Text.RegularExpressions;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Api
{
    internal class FlightNumberApiWrapper: IFlightNumberApiWrapper
    {
        private readonly Regex _regex = new Regex(@"^([A-Z]{3})(\d+)([A-Z]*)$", RegexOptions.Compiled);

        private readonly ITrackerLogger _logger;
        private readonly IAirlineApiWrapper _airlineApiWrapper;
        private readonly ITrackedAircraftWriter _trackedAircraftWriter;

        public FlightNumberApiWrapper(
            ITrackerLogger logger,
            IAirlineApiWrapper airlineApiWrapper,
            ITrackedAircraftWriter trackedAircraftWriter)
        {
            _logger = logger;
            _airlineApiWrapper = airlineApiWrapper;
            _trackedAircraftWriter = trackedAircraftWriter;
        }

        /// <summary>
        /// Return a flight number given a callsign
        /// </summary>
        /// <param name="callsign"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public async Task<FlightNumber> GetFlightNumberFromCallsignAsync(string callsign, DateTime? timestamp = null)
        {
            // Attempt to build a flight number from the callsign
            var flightNumber = await BuildFlightNumberAsync(callsign);

            // If successful, construct a flight number object
            FlightNumber number = !string.IsNullOrEmpty(flightNumber) ?
                new()
                {
                    Callsign = callsign,
                    Number = flightNumber,
                    Date = timestamp
                } : null;

            return number;
        }

        /// <summary>
        /// Get flight numbers for aircraft that are currently being tracked
        /// </summary>
        /// <param name="statuses"></param>
        /// <returns></returns>
        public async Task<List<FlightNumber>> GetFlightNumbersForTrackedAircraftAsync(IEnumerable<TrackingStatus> statuses)
        {
            List<FlightNumber> numbers = [];

            // Get a list of tracked aircraft that have the callsign set and match the status requirements
            var trackedAircraft = await _trackedAircraftWriter.ListAsync(x =>
                (x.Callsign != null) &&
                ((statuses.Count() == 0) || statuses.Contains(x.Status)));

            if (trackedAircraft?.Count > 0)
            {
                // Iterate over the list of tracked aircraft
                foreach (var aircraft in trackedAircraft)
                {
                    // Build a flight number for this aircraft and, if successful, add it to the list
                    var number = await GetFlightNumberFromCallsignAsync(aircraft.Callsign, aircraft.LastSeen);
                    if (number != null)
                    {
                        numbers.Add(number);
                    }
                }
            }
            else
            {
                _logger.LogMessage(Severity.Warning, $"No tracked aircraft found with the callsign set");
            }

            return numbers;
        }

        /// <summary>
        /// Convert an ICAO callsign (e.g., "BAW2777", "EZY45TQ") to an IATA flight number (e.g., "BA2777", "U245").
        /// Returns null if the callsign doesn't match the expected pattern or the ICAO code isn't in the map
        /// </summary>
        private async Task<string> BuildFlightNumberAsync(string callsign)
        {
            // Check the callsign contains some information
            if (string.IsNullOrEmpty(callsign))
            {
                _logger.LogMessage(Severity.Debug, "Empty callsign cannot be used to build a flight number");
                return null;
            }

            // Normalise the callsign by trimming it and converting to uppercase
            var normalised = callsign.Trim().ToUpperInvariant();

            // Match it against the Regex - this captures 3 groups - the 3-letter airline ICAO code, the
            // numeric portion and option non-numeric suffixes that may be present or not
            var matches = _regex.Match(normalised);
            if (!matches.Success)
            {
                _logger.LogMessage(Severity.Debug, $"Callsign {callsign} does not match the expected pattern");
                return null;
            }

            // Get the ICAO code and numeric portion, ignoring the suffix
            var icao = matches.Groups[1].Value;
            var number = matches.Groups[2].Value;

            // Attempt to find the airline with the specified ICAO code
            var airline = await _airlineApiWrapper.LookupAirlineAsync(icao, null, null);
            if (airline == null)
            {
                _logger.LogMessage(Severity.Debug, $"Airline with ICAO code {icao} not found");
                return null;
            }

            return $"{airline.IATA}{number}";
        }
    }
}