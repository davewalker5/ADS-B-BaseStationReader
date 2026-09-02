using BaseStationReader.BusinessLogic.Database;
using BaseStationReader.Entities.Api;
using BaseStationReader.Entities.Import;
using BaseStationReader.Entities.Logging;
using BaseStationReader.Interfaces.Database;
using BaseStationReader.Interfaces.DataExchange;

namespace BaseStationReader.BusinessLogic.Import
{
    public class AirlineCallsignPrefixImporter :
        CsvImporter<AirlineCallsignPrefixMappingProfile, AirlineCallsignPrefix>,
        IAirlineCallsignPrefixImporter
    {
        private readonly IDatabaseManagementFactory _factory;

        public AirlineCallsignPrefixImporter(IDatabaseManagementFactory factory) : base(factory.Logger)
            => _factory = factory;

        /// <inheritdoc />
        public override async Task SaveAsync(IEnumerable<AirlineCallsignPrefix> mappings)
        {
            var input = mappings?.ToList() ?? [];
            if (input.Count == 0)
            {
                Logger.LogMessage(Severity.Warning, "No airline callsign prefix mappings to save");
                return;
            }

            Logger.LogMessage(Severity.Info,
                $"Validating {input.Count} airline callsign prefix mappings");

            var airlines = await _factory.AirlineManager.ListAsync(x => true);
            var airlineGroups = airlines
                .Where(x => !string.IsNullOrWhiteSpace(x.ICAO))
                .GroupBy(x => StringCleaner.CleanICAO(x.ICAO), StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.Ordinal);
            var provenanceRecords = await _factory.ProvenanceManager.ListAsync(x => true);
            var provenanceGroups = provenanceRecords
                .GroupBy(x => x.SourceRef, StringComparer.Ordinal)
                .ToDictionary(x => x.Key, x => x.ToList(), StringComparer.Ordinal);

            var missingAirlines = new HashSet<string>(StringComparer.Ordinal);
            var ambiguousAirlines = new HashSet<string>(StringComparer.Ordinal);
            var missingProvenance = new HashSet<string>(StringComparer.Ordinal);
            var ambiguousProvenance = new HashSet<string>(StringComparer.Ordinal);

            foreach (var mapping in input)
            {
                mapping.Prefix = StringCleaner.CleanCallsignPrefix(mapping.Prefix);
                mapping.AirlineIcaoRef = StringCleaner.CleanICAO(mapping.AirlineIcaoRef ?? "");
                mapping.ProvenanceRef = mapping.ProvenanceRef?.Trim() ?? "";

                if (!airlineGroups.TryGetValue(mapping.AirlineIcaoRef, out var matchingAirlines))
                {
                    missingAirlines.Add(mapping.AirlineIcaoRef);
                }
                else if (matchingAirlines.Count != 1)
                {
                    ambiguousAirlines.Add(mapping.AirlineIcaoRef);
                }
                else
                {
                    mapping.AirlineId = matchingAirlines[0].Id;
                }

                if (!provenanceGroups.TryGetValue(mapping.ProvenanceRef, out var matchingProvenance))
                {
                    missingProvenance.Add(mapping.ProvenanceRef);
                }
                else if (matchingProvenance.Count != 1)
                {
                    ambiguousProvenance.Add(mapping.ProvenanceRef);
                }
                else
                {
                    mapping.ProvenanceId = matchingProvenance[0].Id;
                }
            }

            ThrowForReferenceErrors(
                missingAirlines, ambiguousAirlines, missingProvenance, ambiguousProvenance);

            var duplicateConflicts = input
                .GroupBy(x => x.Prefix, StringComparer.Ordinal)
                .Where(group => group
                    .Select(x => (x.AirlineId, x.ProvenanceId))
                    .Distinct()
                    .Skip(1)
                    .Any())
                .Select(group => group.Key)
                .OrderBy(x => x)
                .ToList();
            if (duplicateConflicts.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Conflicting duplicate callsign prefix(es): {string.Join(", ", duplicateConflicts)}");
            }

            var validated = input
                .GroupBy(x => x.Prefix, StringComparer.Ordinal)
                .Select(group => group.First())
                .ToList();
            var existing = await _factory.AirlineCallsignPrefixManager.ListAsync(x => true);
            var existingByPrefix = existing.ToDictionary(x => x.Prefix, StringComparer.Ordinal);
            var databaseConflicts = validated
                .Where(mapping => existingByPrefix.TryGetValue(mapping.Prefix, out var current) &&
                    (current.AirlineId != mapping.AirlineId || current.ProvenanceId != mapping.ProvenanceId))
                .Select(x => x.Prefix)
                .OrderBy(x => x)
                .ToList();
            if (databaseConflicts.Count > 0)
            {
                throw new InvalidOperationException(
                    $"Existing callsign prefix mapping conflict(s): {string.Join(", ", databaseConflicts)}");
            }

            Logger.LogMessage(Severity.Info,
                $"Saving {validated.Count} airline callsign prefix mappings to the database");
            foreach (var mapping in validated)
            {
                Logger.LogMessage(Severity.Debug,
                    $"Saving callsign prefix '{mapping.Prefix}' for airline '{mapping.AirlineIcaoRef}'");
                await _factory.AirlineCallsignPrefixManager.AddAsync(
                    mapping.Prefix, mapping.AirlineId, mapping.ProvenanceId);
            }
        }

        private static void ThrowForReferenceErrors(
            HashSet<string> missingAirlines,
            HashSet<string> ambiguousAirlines,
            HashSet<string> missingProvenance,
            HashSet<string> ambiguousProvenance)
        {
            var errors = new List<string>();
            if (missingAirlines.Count > 0)
            {
                errors.Add($"airline ICAO code(s) not found: {Format(missingAirlines)}");
            }
            if (ambiguousAirlines.Count > 0)
            {
                errors.Add($"ambiguous airline ICAO code(s): {Format(ambiguousAirlines)}");
            }
            if (missingProvenance.Count > 0)
            {
                errors.Add($"provenance record(s) not found: {Format(missingProvenance)}");
            }
            if (ambiguousProvenance.Count > 0)
            {
                errors.Add($"ambiguous provenance reference(s): {Format(ambiguousProvenance)}");
            }
            if (errors.Count > 0)
            {
                throw new InvalidOperationException(string.Join("; ", errors));
            }
        }

        private static string Format(IEnumerable<string> values)
            => string.Join(", ", values.Select(x => string.IsNullOrEmpty(x) ? "<empty>" : x).OrderBy(x => x));
    }
}
