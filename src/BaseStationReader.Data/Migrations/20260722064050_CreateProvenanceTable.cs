using BaseStationReader.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    [DbContext(typeof(BaseStationReaderDbContext))]
    [Migration("20260722064050_CreateProvenanceTable")]
    public partial class CreateProvenanceTable : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "PROVENANCE",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SourceRef = table.Column<string>(type: "TEXT", nullable: false),
                    Source = table.Column<string>(type: "TEXT", nullable: false),
                    SourceUrl = table.Column<string>(type: "TEXT", nullable: false),
                    SourceDataset = table.Column<string>(type: "TEXT", nullable: false),
                    SourceVersion = table.Column<string>(type: "TEXT", nullable: false),
                    Licence = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PROVENANCE", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_PROVENANCE_SourceRef",
                table: "PROVENANCE",
                column: "SourceRef",
                unique: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "PROVENANCE");
        }
    }
}
