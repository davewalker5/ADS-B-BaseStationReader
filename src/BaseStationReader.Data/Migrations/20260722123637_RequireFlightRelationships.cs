using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class RequireFlightRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Repeat the code-to-airport resolution immediately before enforcing the constraints. This also
            // covers databases where airports were added after the nullable relationship columns were introduced.
            migrationBuilder.Sql(
                """
                UPDATE FLIGHT
                SET OriginAirportId = COALESCE(
                    (SELECT Id FROM AIRPORT
                     WHERE ICAO = FLIGHT.Embarkation COLLATE NOCASE LIMIT 1),
                    (SELECT Id FROM AIRPORT
                     WHERE IATA = FLIGHT.Embarkation COLLATE NOCASE LIMIT 1))
                WHERE OriginAirportId IS NULL AND LENGTH(TRIM(Embarkation)) > 0;

                UPDATE FLIGHT
                SET DestinationAirportId = COALESCE(
                    (SELECT Id FROM AIRPORT
                     WHERE ICAO = FLIGHT.Destination COLLATE NOCASE LIMIT 1),
                    (SELECT Id FROM AIRPORT
                     WHERE IATA = FLIGHT.Destination COLLATE NOCASE LIMIT 1))
                WHERE DestinationAirportId IS NULL AND LENGTH(TRIM(Destination)) > 0;

                CREATE TABLE __FlightRelationshipValidation (
                    InvalidRows INTEGER NOT NULL CHECK (InvalidRows = 0));
                INSERT INTO __FlightRelationshipValidation
                SELECT COUNT(*) FROM FLIGHT
                WHERE AirlineId IS NULL OR OriginAirportId IS NULL OR DestinationAirportId IS NULL;
                DROP TABLE __FlightRelationshipValidation;
                """);

            migrationBuilder.DropColumn(
                name: "Destination",
                table: "FLIGHT");

            migrationBuilder.DropColumn(
                name: "Embarkation",
                table: "FLIGHT");

            migrationBuilder.AlterColumn<int>(
                name: "OriginAirportId",
                table: "FLIGHT",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "DestinationAirportId",
                table: "FLIGHT",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);

            migrationBuilder.AlterColumn<int>(
                name: "AirlineId",
                table: "FLIGHT",
                type: "INTEGER",
                nullable: false,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "OriginAirportId",
                table: "FLIGHT",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "DestinationAirportId",
                table: "FLIGHT",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AlterColumn<int>(
                name: "AirlineId",
                table: "FLIGHT",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "FLIGHT",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "Embarkation",
                table: "FLIGHT",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                """
                UPDATE FLIGHT
                SET Embarkation = COALESCE(
                    (SELECT IATA FROM AIRPORT WHERE Id = FLIGHT.OriginAirportId),
                    (SELECT ICAO FROM AIRPORT WHERE Id = FLIGHT.OriginAirportId),
                    ''),
                    Destination = COALESCE(
                    (SELECT IATA FROM AIRPORT WHERE Id = FLIGHT.DestinationAirportId),
                    (SELECT ICAO FROM AIRPORT WHERE Id = FLIGHT.DestinationAirportId),
                    '');
                """);
        }
    }
}
