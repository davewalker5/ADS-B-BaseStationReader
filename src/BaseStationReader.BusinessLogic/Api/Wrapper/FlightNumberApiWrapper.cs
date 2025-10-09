using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Entities.Tracking;
using BaseStationReader.Interfaces.Api;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.Logging;
using BaseStationReader.Interfaces.Tracking;

namespace BaseStationReader.BusinessLogic.Api
{
    internal class FlightNumberApiWrapper : IFlightNumberApiWrapper
    {
        private readonly ITrackerLogger _logger;
        private readonly IDatabaseManagementFactory _factory;
        private readonly ITrackedAircraftWriter _trackedAircraftWriter;

        public FlightNumberApiWrapper(
            ITrackerLogger logger,
            IDatabaseManagementFactory factory,
            ITrackedAircraftWriter trackedAircraftWriter)
        {
            _logger = logger;
            _factory = factory;
            _trackedAircraftWriter = trackedAircraftWriter;
        }

        /// <summary>
        /// Infer a flight number given a callsign
        /// </summary>
        /// <param name="callsign"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public async Task<FlightNumber> GetFlightNumberFromCallsignAsync(string callsign, DateTime? timestamp = null)
        {
            var flightNumber = new FlightNumber(callsign, null, timestamp);

            // Look for a flight number mapping for the callsign
            var mapping = await _factory.ConfirmedMappingManager.GetAsync(x => x.Callsign == callsign);
            if (mapping != null)
            {
                _logger.LogMessage(Severity.Debug, $"Flight number mapping found for {callsign} => {mapping.FlightIATA}");
                flightNumber.Number = mapping.FlightIATA;
            }
            else
            {
                _logger.LogMessage(Severity.Debug, $"No flight number mapping found for '{callsign}'");
            }

            return flightNumber;
        }

        /// <summary>
        /// Infer a flight number for each callsign in the supplied list
        /// </summary>
        /// <param name="callsigns"></param>
        /// <param name="timestamp"></param>
        /// <returns></returns>
        public async Task<List<FlightNumber>> GetFlightNumbersFromCallsignsAsync(IEnumerable<string> callsigns, DateTime? timestamp = null)
        {
            List<FlightNumber> numbers = [];

            // Iterate over the list of callsigns
            foreach (var callsign in callsigns)
            {
                // Infer a flight number from this callsign and add the result to the list
                var number = await GetFlightNumberFromCallsignAsync(callsign, timestamp);
                numbers.Add(number);
            }

            return numbers;
        }

        /// <summary>
        /// Infer flight numbers for aircraft that are currently being tracked
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
                    // Infer a flight number from this callsign and add the result to the list
                    var number = await GetFlightNumberFromCallsignAsync(aircraft.Callsign, aircraft.LastSeen);
                    numbers.Add(number);
                }
            }
            else
            {
                _logger.LogMessage(Severity.Warning, $"No tracked aircraft found with the callsign set");
            }

            return numbers;
        }
    }
}