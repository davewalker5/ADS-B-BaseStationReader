using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AircraftModelLookup : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
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
                name: "WAKE_TURBULENCE_CATEGORY",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Category = table.Column<string>(type: "TEXT", nullable: false),
                    Meaning = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WAKE_TURBULENCE_CATEGORY", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "AIRCRAFT_MODEL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ManufacturerId = table.Column<int>(type: "INTEGER", nullable: false),
                    WakeTurbulenceCategoryId = table.Column<int>(type: "INTEGER", nullable: true),
                    IATA = table.Column<string>(type: "TEXT", nullable: false),
                    ICAO = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRCRAFT_MODEL", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIRCRAFT_MODEL_MANUFACTURER_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "MANUFACTURER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                    table.ForeignKey(
                        name: "FK_AIRCRAFT_MODEL_WAKE_TURBULENCE_CATEGORY_WakeTurbulenceCategoryId",
                        column: x => x.WakeTurbulenceCategoryId,
                        principalTable: "WAKE_TURBULENCE_CATEGORY",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIRCRAFT_POSITION_AircraftId",
                table: "AIRCRAFT_POSITION",
                column: "AircraftId");

            migrationBuilder.CreateIndex(
                name: "IX_AIRCRAFT_MODEL_ManufacturerId",
                table: "AIRCRAFT_MODEL",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_AIRCRAFT_MODEL_WakeTurbulenceCategoryId",
                table: "AIRCRAFT_MODEL",
                column: "WakeTurbulenceCategoryId");

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
                name: "AIRCRAFT_MODEL");

            migrationBuilder.DropTable(
                name: "MANUFACTURER");

            migrationBuilder.DropTable(
                name: "WAKE_TURBULENCE_CATEGORY");

            migrationBuilder.DropIndex(
                name: "IX_AIRCRAFT_POSITION_AircraftId",
                table: "AIRCRAFT_POSITION");
        }
    }
}
