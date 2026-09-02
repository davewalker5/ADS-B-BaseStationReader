using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    [DbContext(typeof(BaseStationReaderDbContext))]
    [Migration("20260902230000_AllowExactAirlineCallsignPrefixMappings")]
    public partial class AllowExactAirlineCallsignPrefixMappings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
            => ReplaceView(migrationBuilder, allowExactMappings: true);

        protected override void Down(MigrationBuilder migrationBuilder)
            => ReplaceView(migrationBuilder, allowExactMappings: false);

        private static void ReplaceView(MigrationBuilder migrationBuilder, bool allowExactMappings)
        {
            var mappedCallsignCondition = allowExactMappings
                ? "prefix_mapping.Id IS NOT NULL"
                : """
                  (prefix_mapping.Id IS NOT NULL
                   AND LENGTH(tracked.Callsign) > LENGTH(prefix_mapping.Prefix)
                   AND SUBSTR(tracked.Callsign, LENGTH(prefix_mapping.Prefix) + 1) GLOB '*[0-9]*')
                  """;

            migrationBuilder.Sql(
                $$"""
                DROP VIEW SIGHTING;

                CREATE VIEW SIGHTING AS
                SELECT
                    tracked.Id AS Id,
                    aircraft.Id AS AircraftId,
                    flight.Id AS FlightId,
                    COALESCE(flight.AirlineId, mapped_airline.Id, fallback_airline.Id) AS AirlineId,
                    tracked.FirstSeen AS Timestamp
                FROM TRACKED_AIRCRAFT AS tracked
                INNER JOIN AIRCRAFT AS aircraft
                    ON aircraft.Id = (
                        SELECT candidate.Id
                        FROM AIRCRAFT AS candidate
                        WHERE candidate.Address = tracked.Address
                        ORDER BY candidate.Id
                        LIMIT 1)
                LEFT JOIN FLIGHT AS flight
                    ON flight.Id = (
                        SELECT candidate.Id
                        FROM FLIGHT AS candidate
                        WHERE candidate.Callsign = tracked.Callsign
                        ORDER BY candidate.Id
                        LIMIT 1)
                LEFT JOIN AIRLINE_CALLSIGN_PREFIX AS prefix_mapping
                    ON flight.AirlineId IS NULL
                   AND prefix_mapping.Id = (
                        SELECT candidate.Id
                        FROM AIRLINE_CALLSIGN_PREFIX AS candidate
                        WHERE candidate.Prefix IN (
                            SUBSTR(tracked.Callsign, 1, 1),
                            SUBSTR(tracked.Callsign, 1, 2),
                            SUBSTR(tracked.Callsign, 1, 3),
                            SUBSTR(tracked.Callsign, 1, 4),
                            SUBSTR(tracked.Callsign, 1, 5),
                            SUBSTR(tracked.Callsign, 1, 6),
                            SUBSTR(tracked.Callsign, 1, 7),
                            SUBSTR(tracked.Callsign, 1, 8))
                        ORDER BY LENGTH(candidate.Prefix) DESC, candidate.Id
                        LIMIT 1)
                LEFT JOIN AIRLINE AS mapped_airline
                    ON mapped_airline.Id = prefix_mapping.AirlineId
                LEFT JOIN AIRLINE AS fallback_airline
                    ON flight.AirlineId IS NULL
                   AND prefix_mapping.Id IS NULL
                   AND fallback_airline.Id = (
                        SELECT candidate.Id
                        FROM AIRLINE AS candidate
                        WHERE candidate.ICAO = SUBSTR(tracked.Callsign, 1, 3)
                        ORDER BY candidate.Id
                        LIMIT 1)
                WHERE tracked.Address <> '000000'
                  AND tracked.Callsign IS NOT NULL
                  AND tracked.Callsign <> ''
                  AND (
                      flight.Id IS NOT NULL
                      OR {{mappedCallsignCondition}}
                      OR (
                          fallback_airline.Id IS NOT NULL
                          AND LENGTH(tracked.Callsign) > 3
                          AND SUBSTR(tracked.Callsign, 4) GLOB '*[0-9]*'))
                  AND NOT EXISTS (
                      SELECT 1
                      FROM EXCLUDED_ADDRESS AS excluded
                      WHERE excluded.Address = tracked.Address)
                  AND NOT EXISTS (
                      SELECT 1
                      FROM EXCLUDED_CALLSIGN AS excluded
                      WHERE excluded.Callsign = tracked.Callsign);
                """);
        }
    }
}
