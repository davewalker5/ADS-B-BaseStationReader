using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class ConfirmedMappings : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "CONFIRMED_MAPPING",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AirlineICAO = table.Column<string>(type: "TEXT", nullable: false),
                    AirlineIATA = table.Column<string>(type: "TEXT", nullable: false),
                    FlightIATA = table.Column<string>(type: "TEXT", nullable: false),
                    Callsign = table.Column<string>(type: "TEXT", nullable: false),
                    Digits = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CONFIRMED_MAPPING", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "CONFIRMED_MAPPING");
        }
    }
}
