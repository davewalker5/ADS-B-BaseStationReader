using System.Text.RegularExpressions;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Config;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;

namespace BaseStationReader.BusinessLogic.Api.Wrapper
{
    public class LookupEligibilityAssessor : ILookupEligibilityAssessor
    {
        private static readonly Regex _addressRegex = new(@"^[A-Za-z0-9]{6}$", RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private readonly ITrackerLogger _logger;
        private readonly IHistoricalFlightApiWrapper _historicalFlightApiWrapper;
        private readonly IActiveFlightApiWrapper _activeFlightApiWrapper;
        private readonly IDatabaseManagementFactory _factory;
        private readonly bool _ignoreTrackingStatus;

        public LookupEligibilityAssessor(
            ITrackerLogger logger,
            IHistoricalFlightApiWrapper historicalFlightApiWrapper,
            IActiveFlightApiWrapper activeFlightApiWrapper,
            IDatabaseManagementFactory factory,
            bool ignoreTrackingStatus)
        {
            _logger = logger;
            _historicalFlightApiWrapper = historicalFlightApiWrapper;
            _activeFlightApiWrapper = activeFlightApiWrapper;
            _factory = factory;
            _ignoreTrackingStatus = ignoreTrackingStatus;
        }

        /// <summary>
        /// Check that an aircraft is eligible for lookup
        /// </summary>
        /// <param name="type"></param>
        /// <param name="address"></param>
        /// <returns></returns>
        public async Task<EligibilityResult> IsEligibleForLookupAsync(ApiEndpointType type, string address)
        {
            _logger.LogMessage(Severity.Debug, $"Assessing eligibility of aircraft with address {address} for lookup");

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
                _logger.LogMessage(Severity.Debug, $"{address} is eligible : Flight lookup API supports lookup by aircraft address");
                return new(true, true);
            }

            // If tracking status is to be ignored, the aircraft is eligible at this point
            if (_ignoreTrackingStatus)
            {
                _logger.LogMessage(Severity.Debug, $"{address} is eligible : Ignoring eligibility criteria based on aircraft tracking status");
                return new(true, true);
            }

            // Load the tracked aircraft record for further validation
            var aircraft = await _factory.TrackedAircraftWriter.GetLookupCandidateAsync(address);
            if (aircraft == null)
            {
                _logger.LogMessage(Severity.Warning, $"{address} is not eligible : Aircraft is not a valid candidate for lookup");
                return new(false, true);
            }

            // Attempt to load the flight number/callsign mapping for the aircraft
            var mapping = await _factory.FlightNumberMappingManager.GetAsync(x => x.Callsign == aircraft.Callsign);
            if (mapping == null)
            {
                _logger.LogMessage(Severity.Warning, $"{address} is not eligible : Flight number mapping for callsign {aircraft.Callsign} not found");
                return new(false, false);
            }

            // If we make it here, it's eligible
            _logger.LogMessage(Severity.Debug, $"{address} is eligible : Flight number mapping for callsign {aircraft.Callsign} found");
            return new(true, true);
        }
    }
}