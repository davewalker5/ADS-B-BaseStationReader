using BaseStationReader.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    [DbContext(typeof(BaseStationReaderDbContext))]
    [Migration("20260727160000_ReplaceSightingTableWithView")]
    public class ReplaceSightingTableWithView : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "SIGHTING");

            migrationBuilder.Sql(
                """
                CREATE VIEW SIGHTING AS
                SELECT
                    tracked.Id AS Id,
                    aircraft.Id AS AircraftId,
                    flight.Id AS FlightId,
                    tracked.FirstSeen AS Timestamp
                FROM TRACKED_AIRCRAFT AS tracked
                INNER JOIN AIRCRAFT AS aircraft
                    ON aircraft.Id = (
                        SELECT candidate.Id
                        FROM AIRCRAFT AS candidate
                        WHERE candidate.Address = tracked.Address
                        ORDER BY candidate.Id
                        LIMIT 1)
                INNER JOIN FLIGHT AS flight
                    ON flight.Id = (
                        SELECT candidate.Id
                        FROM FLIGHT AS candidate
                        WHERE candidate.Callsign = tracked.Callsign
                        ORDER BY candidate.Id
                        LIMIT 1)
                WHERE tracked.Address <> '000000'
                  AND tracked.Callsign IS NOT NULL
                  AND tracked.Callsign <> ''
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

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql(
                """
                CREATE TEMPORARY TABLE SIGHTING_DOWN AS
                SELECT Id, AircraftId, FlightId, Timestamp
                FROM SIGHTING;

                DROP VIEW SIGHTING;
                """);

            migrationBuilder.CreateTable(
                name: "SIGHTING",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AircraftId = table.Column<int>(type: "INTEGER", nullable: false),
                    FlightId = table.Column<int>(type: "INTEGER", nullable: false),
                    Timestamp = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SIGHTING", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SIGHTING_AIRCRAFT_AircraftId",
                        column: x => x.AircraftId,
                        principalTable: "AIRCRAFT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SIGHTING_FLIGHT_FlightId",
                        column: x => x.FlightId,
                        principalTable: "FLIGHT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SIGHTING_AircraftId",
                table: "SIGHTING",
                column: "AircraftId");

            migrationBuilder.CreateIndex(
                name: "IX_SIGHTING_FlightId",
                table: "SIGHTING",
                column: "FlightId");

            migrationBuilder.Sql(
                """
                INSERT INTO SIGHTING (Id, AircraftId, FlightId, Timestamp)
                SELECT Id, AircraftId, FlightId, Timestamp
                FROM SIGHTING_DOWN;

                DROP TABLE SIGHTING_DOWN;
                """);
        }
    }
}
