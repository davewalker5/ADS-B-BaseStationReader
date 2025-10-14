using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class FlightNumberMappingAirports : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Destination",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Embarkation",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "TEXT",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Destination",
                table: "FLIGHT_NUMBER_MAPPING");

            migrationBuilder.DropColumn(
                name: "Embarkation",
                table: "FLIGHT_NUMBER_MAPPING");
        }
    }
}
