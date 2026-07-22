using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddModelProvenanceAndNullableCodes : Migration
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
                table: "MODEL",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.Sql("""
                UPDATE MODEL
                SET ProvenanceId = (SELECT Id FROM PROVENANCE WHERE SourceRef = 'LOCAL');
                """);

            migrationBuilder.AlterColumn<string>(
                name: "ICAO",
                table: "MODEL",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "IATA",
                table: "MODEL",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.CreateIndex(
                name: "IX_MODEL_ProvenanceId",
                table: "MODEL",
                column: "ProvenanceId");

            migrationBuilder.AddForeignKey(
                name: "FK_MODEL_PROVENANCE_ProvenanceId",
                table: "MODEL",
                column: "ProvenanceId",
                principalTable: "PROVENANCE",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_MODEL_PROVENANCE_ProvenanceId",
                table: "MODEL");

            migrationBuilder.DropIndex(
                name: "IX_MODEL_ProvenanceId",
                table: "MODEL");

            migrationBuilder.DropColumn(
                name: "ProvenanceId",
                table: "MODEL");

            migrationBuilder.AlterColumn<string>(
                name: "ICAO",
                table: "MODEL",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "IATA",
                table: "MODEL",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
