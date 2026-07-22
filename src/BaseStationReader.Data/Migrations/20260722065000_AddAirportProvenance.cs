using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAirportProvenance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.Sql("""
                INSERT INTO PROVENANCE (SourceRef, Source, SourceUrl, SourceDataset, SourceVersion, Licence)
                SELECT 'LOCAL', 'N/A', 'N/A', 'N/A', 'N/A', 'N/A'
                WHERE NOT EXISTS (SELECT 1 FROM PROVENANCE WHERE SourceRef = 'LOCAL');
                """);

            migrationBuilder.AddColumn<int>(
                name: "ProvenanceId",
                table: "AIRPORT",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE AIRPORT
                SET ProvenanceId = (SELECT Id FROM PROVENANCE WHERE SourceRef = 'LOCAL');
                """);

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "AIRPORT");

            migrationBuilder.CreateIndex(
                name: "IX_AIRPORT_ProvenanceId",
                table: "AIRPORT",
                column: "ProvenanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_AIRPORT_PROVENANCE_ProvenanceId",
                table: "AIRPORT",
                column: "ProvenanceId",
                principalTable: "PROVENANCE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AIRPORT_PROVENANCE_ProvenanceId",
                table: "AIRPORT");

            migrationBuilder.DropIndex(
                name: "IX_AIRPORT_ProvenanceId",
                table: "AIRPORT");

            migrationBuilder.DropColumn(
                name: "ProvenanceId",
                table: "AIRPORT");

            migrationBuilder.AddColumn<double>(
                name: "Distance",
                table: "AIRPORT",
                type: "REAL",
                nullable: false,
                defaultValue: 0.0);
        }
    }
}
