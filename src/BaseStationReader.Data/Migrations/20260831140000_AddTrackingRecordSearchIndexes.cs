using BaseStationReader.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <summary>
    /// Adds lookup indexes used by tracking-record registration, airline and flight searches.
    /// </summary>
    [DbContext(typeof(BaseStationReaderDbContext))]
    [Migration("20260831140000_AddTrackingRecordSearchIndexes")]
    public partial class AddTrackingRecordSearchIndexes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_AIRCRAFT_Address",
                table: "AIRCRAFT",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_FLIGHT_Callsign",
                table: "FLIGHT",
                column: "Callsign");

            migrationBuilder.CreateIndex(
                name: "IX_AIRLINE_ICAO",
                table: "AIRLINE",
                column: "ICAO");

            migrationBuilder.CreateIndex(
                name: "IX_EXCLUDED_ADDRESS_Address",
                table: "EXCLUDED_ADDRESS",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_EXCLUDED_CALLSIGN_Callsign",
                table: "EXCLUDED_CALLSIGN",
                column: "Callsign");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_AIRCRAFT_Address",
                table: "AIRCRAFT");

            migrationBuilder.DropIndex(
                name: "IX_FLIGHT_Callsign",
                table: "FLIGHT");

            migrationBuilder.DropIndex(
                name: "IX_AIRLINE_ICAO",
                table: "AIRLINE");

            migrationBuilder.DropIndex(
                name: "IX_EXCLUDED_ADDRESS_Address",
                table: "EXCLUDED_ADDRESS");

            migrationBuilder.DropIndex(
                name: "IX_EXCLUDED_CALLSIGN_Callsign",
                table: "EXCLUDED_CALLSIGN");
        }
    }
}
