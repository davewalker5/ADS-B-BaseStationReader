using System.Text.RegularExpressions;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Heuristics;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Api
{
    internal class FlightNumberApiWrapper: IFlightNumberApiWrapper
    {
        private static readonly Regex Rx = new(@"^([A-Z]{3})(\d+)([A-Z]*)$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly InferenceOptions _options;
        private readonly ITrackerLogger _logger;
        private readonly IDatabaseManagementFactory _factory;
        private readonly ITrackedAircraftWriter _trackedAircraftWriter;

        public FlightNumberApiWrapper(
            InferenceOptions options,
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            ITrackedAircraftWriter trackedAircraftWriter)
        {
            _options = options;
            _logger = logger;
            _factory = factory;
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
            var flightNumber = await InferFlightNumberAsync(callsign);

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
        /// Use the flight number heuristics to infer the flight number from the callsign
        /// </summary>
        /// <param name="callsign"></param>
        /// <returns></returns>
        private async Task<string> InferFlightNumberAsync(string callsign)
        {
            // Check the callsign contains some information
            if (string.IsNullOrEmpty(callsign))
            {
                _logger.LogMessage(Severity.Debug, "Empty callsign cannot be used to build a flight number");
                return null;
            }

            // Look for a confirmed mapping between the callsign and flight number and it one is found return it
            var mapping = await _factory.ConfirmedMappingManager.GetAsync(x => x.Callsign == callsign);
            if (mapping != null)
            {
                _logger.LogMessage(Severity.Debug, $"Confirmed mapping found for {callsign} => {mapping.FlightIATA}");
                return mapping.FlightIATA;
            }

            // Parse the callsign:
            //
            // Group 0 - the entire match
            // Group 1 - the airline ICAO code
            // Group 2 - the numeric section of the callsign
            // Group 3 - the suffix (if present
            //
            var matches = Rx.Match(callsign);
            var icao = matches.Groups[1].Value;
            var numericString = matches.Groups[2].Value;
            var suffix = matches.Groups[3].Value;

            // Check the numeric section is actually numeric
            if (!int.TryParse(numericString, out int numeric))
            {
                _logger.LogMessage(Severity.Debug, $"Numeric part of the callsign is not a valid integer: {numericString}");
                return null;
            }

            // Load the airline constants
            var constants = await _factory.AirlineConstantsManager.GetAsync(x => x.AirlineICAO == icao);
            if (constants == null)
            {
                _logger.LogMessage(Severity.Debug, $"No airline constants found for airline with ICAO code {icao}");
                return null;
            }

            // Look for a number/suffix rule
            var numberSuffixRule = await _factory.NumberSuffixRuleManager.GetAsync(x =>
                (x.AirlineICAO == icao) &&
                (x.Numeric == numericString) &&
                (x.Suffix == suffix) &&
                (x.Support >= _options.NumericSuffixMinimumSupport) &&
                (x.Purity == _options.NumericSuffixMinimumPurity));
            if (numberSuffixRule != null)
            {
                var flightIATA = $"{numberSuffixRule.AirlineIATA}{numberSuffixRule.Numeric}";
                _logger.LogMessage(Severity.Debug, $"Number/suffix rule found for {callsign} => {flightIATA}");
                return flightIATA;
            }

            // Look for a suffix delta rule
            var suffixDeltaRule = await _factory.SuffixDeltaRuleManager.GetAsync(x =>
                (x.AirlineICAO == icao) &&
                (x.Suffix == suffix) &&
                (x.Support >= _options.SuffixDeltaMinimumSupport) &&
                (x.Purity == _options.SuffixDeltaMinimumPurity));
            if (suffixDeltaRule != null)
            {
                var flightNumber = numeric + suffixDeltaRule.Delta;
                var flightIATA = $"{suffixDeltaRule.AirlineIATA}{flightNumber}";
                _logger.LogMessage(Severity.Debug, $"Suffix/delta rule found for {callsign} => {flightIATA}");
                return flightIATA;
            }

            // Attempt to construct the flight number using the airline constant prefix
            if (!string.IsNullOrEmpty(constants.ConstantPrefix))
            {
                var flightNumber = $"{constants.ConstantPrefix}{numericString}";
                if (int.TryParse(flightNumber, out int _))
                {
                    var flightIATA = $"{constants.AirlineIATA}{flightNumber}";
                    _logger.LogMessage(Severity.Debug, $"Constant prefix rule found for {callsign} => {flightIATA}");
                    return flightIATA;
                }
            }

            // Attempt to construct the flight number using the airline constant delta
            if (constants.ConstantDelta != null)
            {
                var flightNumber = numeric + constants.ConstantDelta;
                if (flightNumber > 0)
                {
                    var flightIATA = $"{constants.AirlineIATA}{flightNumber}";
                    _logger.LogMessage(Severity.Debug, $"Constant delta rule found for {callsign} => {flightIATA}");
                    return flightIATA;
                }
            }

            // Fallback to using identity mapping
            if (constants.IdentityRate >= _options.MinimumIdentityPurity)
            {
                var flightIATA = $"{constants.AirlineIATA}{numericString}";
                _logger.LogMessage(Severity.Debug, $"Identity mapping rule found for {callsign} => {flightIATA}");
                return flightIATA;
            }

            // Can't construct a flight number with any confidence
            return null;
        }
    }
}