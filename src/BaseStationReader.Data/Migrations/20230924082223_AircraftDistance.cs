using Microsoft.EntityFrameworkCore.Migrations;
using System.Diagnostics.CodeAnalysis;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AircraftDistance : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<double>(
                name: "Distance",
                table: "AIRCRAFT_POSITION",
                type: "REAL",
                nullable: true);

            migrationBuilder.AddColumn<double>(
                name: "Distance",
                table: "AIRCRAFT",
                type: "REAL",
                nullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Distance",
                table: "AIRCRAFT_POSITION");

            migrationBuilder.DropColumn(
                name: "Distance",
                table: "AIRCRAFT");
        }
    }
}
