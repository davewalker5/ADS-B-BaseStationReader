using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAirlineProvenance : Migration
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
                table: "AIRLINE",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE AIRLINE
                SET ProvenanceId = (SELECT Id FROM PROVENANCE WHERE SourceRef = 'LOCAL');
                """);

            migrationBuilder.CreateIndex(
                name: "IX_AIRLINE_ProvenanceId",
                table: "AIRLINE",
                column: "ProvenanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_AIRLINE_PROVENANCE_ProvenanceId",
                table: "AIRLINE",
                column: "ProvenanceId",
                principalTable: "PROVENANCE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AIRLINE_PROVENANCE_ProvenanceId",
                table: "AIRLINE");

            migrationBuilder.DropIndex(
                name: "IX_AIRLINE_ProvenanceId",
                table: "AIRLINE");

            migrationBuilder.DropColumn(
                name: "ProvenanceId",
                table: "AIRLINE");
        }
    }
}
