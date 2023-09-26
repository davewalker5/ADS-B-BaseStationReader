using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AircraftModelLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIRLINE",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    IATA = table.Column<string>(type: "TEXT", nullable: false),
                    ICAO = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRLINE", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MANUFACTURER",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MANUFACTURER", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "MODEL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ManufacturerId = table.Column<int>(type: "INTEGER", nullable: false),
                    IATA = table.Column<string>(type: "TEXT", nullable: false),
                    ICAO = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MODEL", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MODEL_MANUFACTURER_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "MANUFACTURER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIRCRAFT_POSITION_AircraftId",
                table: "AIRCRAFT_POSITION",
                column: "AircraftId");

            migrationBuilder.CreateIndex(
                name: "IX_MODEL_ManufacturerId",
                table: "MODEL",
                column: "ManufacturerId");

            migrationBuilder.AddForeignKey(
                name: "FK_AIRCRAFT_POSITION_AIRCRAFT_AircraftId",
                table: "AIRCRAFT_POSITION",
                column: "AircraftId",
                principalTable: "AIRCRAFT",
                principalColumn: "Id",
                onDelete: ReferentialAction.Cascade);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_AIRCRAFT_POSITION_AIRCRAFT_AircraftId",
                table: "AIRCRAFT_POSITION");

            migrationBuilder.DropTable(
                name: "AIRLINE");

            migrationBuilder.DropTable(
                name: "MODEL");

            migrationBuilder.DropTable(
                name: "MANUFACTURER");

            migrationBuilder.DropIndex(
                name: "IX_AIRCRAFT_POSITION_AircraftId",
                table: "AIRCRAFT_POSITION");
        }
    }
}
