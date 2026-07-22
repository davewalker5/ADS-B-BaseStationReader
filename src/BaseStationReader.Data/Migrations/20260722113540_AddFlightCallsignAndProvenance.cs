using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddFlightCallsignAndProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO PROVENANCE (SourceRef, Source, SourceUrl, SourceDataset, SourceVersion, Licence)
                SELECT 'LOCAL', 'N/A', 'N/A', 'N/A', 'N/A', 'N/A'
                WHERE NOT EXISTS (SELECT 1 FROM PROVENANCE WHERE SourceRef = 'LOCAL');
                """);

            migrationBuilder.AddColumn<string>(
                name: "Callsign",
                table: "FLIGHT",
                type: "TEXT",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<int>(
                name: "ProvenanceId",
                table: "FLIGHT",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE FLIGHT
                SET Callsign = COALESCE(NULLIF(TRIM(ICAO), ''), IATA),
                    ProvenanceId = (SELECT Id FROM PROVENANCE WHERE SourceRef = 'LOCAL');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_FLIGHT_ProvenanceId",
                table: "FLIGHT",
                column: "ProvenanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_FLIGHT_PROVENANCE_ProvenanceId",
                table: "FLIGHT",
                column: "ProvenanceId",
                principalTable: "PROVENANCE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_FLIGHT_PROVENANCE_ProvenanceId",
                table: "FLIGHT");

            migrationBuilder.DropIndex(
                name: "IX_FLIGHT_ProvenanceId",
                table: "FLIGHT");

            migrationBuilder.DropColumn(
                name: "Callsign",
                table: "FLIGHT");

            migrationBuilder.DropColumn(
                name: "ProvenanceId",
                table: "FLIGHT");
        }
    }
}
