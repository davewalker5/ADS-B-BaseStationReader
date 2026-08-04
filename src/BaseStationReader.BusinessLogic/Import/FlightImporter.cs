using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;

namespace BaseStationReader.BusinessLogic.Import
{
    public class FlightImporter : CsvImporter<FlightMappingProfile, Flight>, IFlightImporter
    {
        private readonly IDatabaseManagementFactory _factory;

        public FlightImporter(IDatabaseManagementFactory factory) : base(factory.Logger)
            => _factory = factory;

        /// <inheritdoc />
        public override List<Flight> Read(string filePath)
        {
            var flights = base.Read(filePath);
            if (flights?.Count > 0)
            {
                foreach (var flight in flights)
                {
                    flight.Callsign = CleanCode(flight.Callsign);
                    flight.IATA = CleanCode(flight.IATA);
                    flight.ICAO = CleanCode(flight.ICAO);
                    flight.OriginICAO = CleanCode(flight.OriginICAO);
                    flight.OriginIATA = CleanCode(flight.OriginIATA);
                    flight.DestinationICAO = CleanCode(flight.DestinationICAO);
                    flight.DestinationIATA = CleanCode(flight.DestinationIATA);
                    flight.AirlineICAO = CleanCode(flight.AirlineICAO);
                    flight.AirlineIATA = CleanCode(flight.AirlineIATA);
                    flight.ProvenanceRef = flight.ProvenanceRef?.Trim() ?? "";
                }

                flights.RemoveAll(x => string.IsNullOrWhiteSpace(x.Callsign));
                Logger.LogMessage(Severity.Info,
                    $"Flights with no callsign removed : {flights.Count} flights remaining");

                flights.RemoveAll(x => string.IsNullOrWhiteSpace(x.IATA));
                Logger.LogMessage(Severity.Info,
                    $"Flights with no Flight IATA code removed : {flights.Count} flights remaining");
            }

            return flights;
        }

        /// <inheritdoc />
        public override async Task SaveAsync(IEnumerable<Flight> flights)
        {
            var rows = flights?.ToList() ?? [];
            if (rows.Count == 0)
            {
                Logger.LogMessage(Severity.Warning, "No flights to save");
                return;
            }

            Logger.LogMessage(Severity.Info, $"Saving {rows.Count} flights to the database");

            var provenance = (await _factory.ProvenanceManager.ListAsync(x => true))
                .GroupBy(x => x.SourceRef, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.Ordinal);
            var airports = await _factory.AirportManager.ListAsync(x => true);
            var airlines = await _factory.AirlineManager.ListAsync(x => true);

            var airportsByIcao = IndexBy(airports, x => x.ICAO);
            var airportsByIata = IndexBy(airports, x => x.IATA);
            var airlinesByIcao = IndexBy(airlines, x => x.ICAO);
            var airlinesByIata = IndexBy(airlines, x => x.IATA);

            var resolved = new List<(Flight Row, Airport Origin, Airport Destination, Airline Airline, Provenance Provenance)>();
            foreach (var row in rows)
            {
                if (!provenance.TryGetValue(row.ProvenanceRef, out var source))
                {
                    throw new InvalidOperationException($"Provenance record not found: {row.ProvenanceRef}");
                }

                var origin = Resolve(row.OriginICAO, row.OriginIATA, airportsByIcao, airportsByIata,
                    "Origin airport", row.Callsign);
                var destination = Resolve(row.DestinationICAO, row.DestinationIATA, airportsByIcao, airportsByIata,
                    "Destination airport", row.Callsign);
                var airline = Resolve(row.AirlineICAO, row.AirlineIATA, airlinesByIcao, airlinesByIata,
                    "Airline", row.Callsign);
                if (origin is null)
                {
                    throw new InvalidOperationException($"An origin airport is required for flight '{row.Callsign}'.");
                }
                if (destination is null)
                {
                    throw new InvalidOperationException($"A destination airport is required for flight '{row.Callsign}'.");
                }
                if (airline is null)
                {
                    throw new InvalidOperationException($"An airline is required for flight '{row.Callsign}'.");
                }
                resolved.Add((row, origin, destination, airline, source));
            }

            foreach (var item in resolved)
            {
                var originCode = PreferredAirportCode(item.Origin);
                var destinationCode = PreferredAirportCode(item.Destination);
                Logger.LogMessage(Severity.Debug,
                    $"Saving flight '{item.Row.Callsign}' : IATA = '{item.Row.IATA}', ICAO = '{item.Row.ICAO}', " +
                    $"Origin = '{originCode}', Destination = '{destinationCode}'");

                await _factory.FlightManager.AddAsync(
                    item.Row.IATA, item.Row.ICAO, item.Row.Callsign,
                    item.Airline.Id, item.Origin.Id, item.Destination.Id, item.Provenance.Id,
                    originCode, destinationCode);
            }
        }

        /// <summary>
        /// Normalises an imported aviation code and removes configured placeholder values.
        /// </summary>
        private string CleanCode(string value)
        {
            var cleaned = value?.Trim() ?? "";
            return Replacements.Contains(cleaned) ? "" : cleaned.ToUpperInvariant();
        }

        /// <summary>
        /// Indexes records by a case-insensitive external code.
        /// </summary>
        private static Dictionary<string, T> IndexBy<T>(IEnumerable<T> records, Func<T, string> selector)
            => records
                .Where(x => !string.IsNullOrWhiteSpace(selector(x)))
                .GroupBy(x => selector(x).Trim(), StringComparer.OrdinalIgnoreCase)
                .ToDictionary(x => x.Key, x => x.First(), StringComparer.OrdinalIgnoreCase);

        /// <summary>
        /// Resolves an imported reference by ICAO code and then by IATA code.
        /// </summary>
        private static T Resolve<T>(
            string icao,
            string iata,
            IReadOnlyDictionary<string, T> byIcao,
            IReadOnlyDictionary<string, T> byIata,
            string label,
            string callsign) where T : class
        {
            if (!string.IsNullOrWhiteSpace(icao))
            {
                if (byIcao.TryGetValue(icao, out var match))
                {
                    return match;
                }
                throw new InvalidOperationException($"{label} ICAO '{icao}' was not found for flight '{callsign}'.");
            }

            if (!string.IsNullOrWhiteSpace(iata))
            {
                if (byIata.TryGetValue(iata, out var match))
                {
                    return match;
                }
                throw new InvalidOperationException($"{label} IATA '{iata}' was not found for flight '{callsign}'.");
            }

            return null;
        }

        /// <summary>
        /// Returns the preferred display code for an airport.
        /// </summary>
        private static string PreferredAirportCode(Airport airport)
            => airport is null ? "" : !string.IsNullOrWhiteSpace(airport.IATA) ? airport.IATA : airport.ICAO;
    }
}
