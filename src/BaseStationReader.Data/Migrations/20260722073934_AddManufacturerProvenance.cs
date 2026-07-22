using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddManufacturerProvenance : Migration
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
                table: "MANUFACTURER",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE MANUFACTURER
                SET ProvenanceId = (SELECT Id FROM PROVENANCE WHERE SourceRef = 'LOCAL');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_MANUFACTURER_ProvenanceId",
                table: "MANUFACTURER",
                column: "ProvenanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_MANUFACTURER_PROVENANCE_ProvenanceId",
                table: "MANUFACTURER",
                column: "ProvenanceId",
                principalTable: "PROVENANCE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MANUFACTURER_PROVENANCE_ProvenanceId",
                table: "MANUFACTURER");

            migrationBuilder.DropIndex(
                name: "IX_MANUFACTURER_ProvenanceId",
                table: "MANUFACTURER");

            migrationBuilder.DropColumn(
                name: "ProvenanceId",
                table: "MANUFACTURER");
        }
    }
}
