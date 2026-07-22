using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightAirportRelationships : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<int>(
                name: "AirlineId",
                table: "FLIGHT",
                type: "INTEGER",
                nullable: true,
                oldClrType: typeof(int),
                oldType: "INTEGER");

            migrationBuilder.AddColumn<int>(
                name: "DestinationAirportId",
                table: "FLIGHT",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "OriginAirportId",
                table: "FLIGHT",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.Sql(
                """
                UPDATE FLIGHT
                SET OriginAirportId = COALESCE(
                    (SELECT Id FROM AIRPORT
                     WHERE ICAO = FLIGHT.Embarkation COLLATE NOCASE LIMIT 1),
                    (SELECT Id FROM AIRPORT
                     WHERE IATA = FLIGHT.Embarkation COLLATE NOCASE LIMIT 1))
                WHERE LENGTH(TRIM(Embarkation)) > 0;

                UPDATE FLIGHT
                SET DestinationAirportId = COALESCE(
                    (SELECT Id FROM AIRPORT
                     WHERE ICAO = FLIGHT.Destination COLLATE NOCASE LIMIT 1),
                    (SELECT Id FROM AIRPORT
                     WHERE IATA = FLIGHT.Destination COLLATE NOCASE LIMIT 1))
                WHERE LENGTH(TRIM(Destination)) > 0;
                """);

            migrationBuilder.CreateIndex(
                name: "IX_FLIGHT_DestinationAirportId",
                table: "FLIGHT",
                column: "DestinationAirportId");

            migrationBuilder.CreateIndex(
                name: "IX_FLIGHT_OriginAirportId",
                table: "FLIGHT",
                column: "OriginAirportId");

            migrationBuilder.AddForeignKey(
                name: "FK_FLIGHT_AIRPORT_DestinationAirportId",
                table: "FLIGHT",
                column: "DestinationAirportId",
                principalTable: "AIRPORT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);

            migrationBuilder.AddForeignKey(
                name: "FK_FLIGHT_AIRPORT_OriginAirportId",
                table: "FLIGHT",
                column: "OriginAirportId",
                principalTable: "AIRPORT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FLIGHT_AIRPORT_DestinationAirportId",
                table: "FLIGHT");

            migrationBuilder.DropForeignKey(
                name: "FK_FLIGHT_AIRPORT_OriginAirportId",
                table: "FLIGHT");

            migrationBuilder.DropIndex(
                name: "IX_FLIGHT_DestinationAirportId",
                table: "FLIGHT");

            migrationBuilder.DropIndex(
                name: "IX_FLIGHT_OriginAirportId",
                table: "FLIGHT");

            migrationBuilder.DropColumn(
                name: "DestinationAirportId",
                table: "FLIGHT");

            migrationBuilder.DropColumn(
                name: "OriginAirportId",
                table: "FLIGHT");

            migrationBuilder.AlterColumn<int>(
                name: "AirlineId",
                table: "FLIGHT",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0,
                oldClrType: typeof(int),
                oldType: "INTEGER",
                oldNullable: true);
        }
    }
}
