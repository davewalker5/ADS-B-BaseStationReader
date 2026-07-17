using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class UnifiedWebUiDatabaseBrowser : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Replace the position foreign-key index with a composite time-ordered index and add browser filters.
            migrationBuilder.DropIndex(
                name: "IX_POSITION_AircraftId",
                table: "POSITION");

            migrationBuilder.CreateIndex(
                name: "IX_TRACKED_AIRCRAFT_Address",
                table: "TRACKED_AIRCRAFT",
                column: "Address");

            migrationBuilder.CreateIndex(
                name: "IX_TRACKED_AIRCRAFT_Callsign",
                table: "TRACKED_AIRCRAFT",
                column: "Callsign");

            migrationBuilder.CreateIndex(
                name: "IX_TRACKED_AIRCRAFT_FirstSeen",
                table: "TRACKED_AIRCRAFT",
                column: "FirstSeen");

            migrationBuilder.CreateIndex(
                name: "IX_TRACKED_AIRCRAFT_LastSeen",
                table: "TRACKED_AIRCRAFT",
                column: "LastSeen");

            migrationBuilder.CreateIndex(
                name: "IX_TRACKED_AIRCRAFT_Status",
                table: "TRACKED_AIRCRAFT",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_POSITION_AircraftId_Timestamp",
                table: "POSITION",
                columns: new[] { "AircraftId", "Timestamp" });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Remove the browser indexes and restore the original position foreign-key index.
            migrationBuilder.DropIndex(
                name: "IX_TRACKED_AIRCRAFT_Address",
                table: "TRACKED_AIRCRAFT");

            migrationBuilder.DropIndex(
                name: "IX_TRACKED_AIRCRAFT_Callsign",
                table: "TRACKED_AIRCRAFT");

            migrationBuilder.DropIndex(
                name: "IX_TRACKED_AIRCRAFT_FirstSeen",
                table: "TRACKED_AIRCRAFT");

            migrationBuilder.DropIndex(
                name: "IX_TRACKED_AIRCRAFT_LastSeen",
                table: "TRACKED_AIRCRAFT");

            migrationBuilder.DropIndex(
                name: "IX_TRACKED_AIRCRAFT_Status",
                table: "TRACKED_AIRCRAFT");

            migrationBuilder.DropIndex(
                name: "IX_POSITION_AircraftId_Timestamp",
                table: "POSITION");

            migrationBuilder.CreateIndex(
                name: "IX_POSITION_AircraftId",
                table: "POSITION",
                column: "AircraftId");
        }
    }
}
