using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AircraftDetails : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIRCRAFT_DETAILS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    ModelId = table.Column<int>(type: "INTEGER", nullable: true),
                    AirlineId = table.Column<int>(type: "INTEGER", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRCRAFT_DETAILS", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIRCRAFT_DETAILS_AIRLINE_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "AIRLINE",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_AIRCRAFT_DETAILS_MODEL_ModelId",
                        column: x => x.ModelId,
                        principalTable: "MODEL",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIRCRAFT_DETAILS_AirlineId",
                table: "AIRCRAFT_DETAILS",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_AIRCRAFT_DETAILS_ModelId",
                table: "AIRCRAFT_DETAILS",
                column: "ModelId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIRCRAFT_DETAILS");
        }
    }
}
