using System.Text.RegularExpressions;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    public class LookupEligibilityAssessor : ILookupEligibilityAssessor
    {
        private static readonly Regex _addressRegex = new(@"^[A-Za-z0-9]{6}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly ITrackerLogger _logger;
        private readonly IHistoricalFlightApiWrapper _historicalFlightApiWrapper;
        private readonly IActiveFlightApiWrapper _activeFlightApiWrapper;
        private readonly IDatabaseManagementFactory _factory;
        private readonly ITrackedAircraftWriter _trackedAircraftWriter;
        private readonly int _maximumLookupAttempts;

        public LookupEligibilityAssessor(
            ITrackerLogger logger,
            IHistoricalFlightApiWrapper historicalFlightApiWrapper,
            IActiveFlightApiWrapper activeFlightApiWrapper,
            IDatabaseManagementFactory factory,
            ITrackedAircraftWriter trackedAircraftWriter,
            int maximumLookupAttempts)
        {
            _logger = logger;
            _historicalFlightApiWrapper = historicalFlightApiWrapper;
            _activeFlightApiWrapper = activeFlightApiWrapper;
            _trackedAircraftWriter = trackedAircraftWriter;
            _factory = factory;
            _maximumLookupAttempts = maximumLookupAttempts;
        }

        /// <summary>
        /// Check that an aircraft is eligible for lookup
        /// </summary>
        /// <param name="type"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<EligibilityResult> IsEligibleForLookup(ApiEndpointType type, string address)
        {
            // Check the aircraft address is valid
            if (!_addressRegex.IsMatch(address))
            {
                _logger.LogMessage(Severity.Warning, $"'{address}' is not a valid aircraft address");
                return new(false, false);
            }

            // If the API supports address-based flight looked, then the address is eligible for lookup
            var supportsAddressLookup = type switch
            {
                ApiEndpointType.HistoricalFlights => _historicalFlightApiWrapper.SupportsLookupBy(ApiProperty.AircraftAddress),
                ApiEndpointType.ActiveFlights => _activeFlightApiWrapper.SupportsLookupBy(ApiProperty.AircraftAddress),
                _ => false
            };

            if (supportsAddressLookup)
            {
                _logger.LogMessage(Severity.Debug, $"Flight lookup API supports lookup by aircraft address");
                return new(true, true);
            }

            // To proceed, we need a tracked aircraft writer and need to load the tracked aircraft record
            // for further validation
            var aircraft = await _trackedAircraftWriter?.GetAsync(x => x.Address == address);
            if (aircraft == null)
            {
                _logger.LogMessage(Severity.Warning, $"No tracked aircraft writer available or the aircraft is not tracked");
                return new(false, false);
            }

            // For lookup by flight number, the aircraft needs to have a callsign that can be mapped to a
            // flight number
            if (string.IsNullOrEmpty(aircraft.Callsign))
            {
                _logger.LogMessage(Severity.Warning, $"Tracked aircraft {address} does not have a callsign to enable flight number lookup");
                return new(false, true);
            }

            // Attempt to load the flight number/callsign mapping for the aircraft
            var mapping = await _factory.ConfirmedMappingManager.GetAsync(x => x.Callsign == aircraft.Callsign);
            if (mapping == null)
            {
                _logger.LogMessage(Severity.Warning, $"Flight number mapping for callsign {aircraft.Callsign} not found");
                return new(false, false);
            }

            // If we make it here, it's eligible
            return new(true, true);
        }
    }
}