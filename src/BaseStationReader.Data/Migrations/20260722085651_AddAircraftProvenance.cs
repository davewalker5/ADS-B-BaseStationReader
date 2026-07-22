using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAircraftProvenance : Migration
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
                table: "AIRCRAFT",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE AIRCRAFT
                SET ProvenanceId = (SELECT Id FROM PROVENANCE WHERE SourceRef = 'LOCAL');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AIRCRAFT_ProvenanceId",
                table: "AIRCRAFT",
                column: "ProvenanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_AIRCRAFT_PROVENANCE_ProvenanceId",
                table: "AIRCRAFT",
                column: "ProvenanceId",
                principalTable: "PROVENANCE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AIRCRAFT_PROVENANCE_ProvenanceId",
                table: "AIRCRAFT");

            migrationBuilder.DropIndex(
                name: "IX_AIRCRAFT_ProvenanceId",
                table: "AIRCRAFT");

            migrationBuilder.DropColumn(
                name: "ProvenanceId",
                table: "AIRCRAFT");
        }
    }
}
