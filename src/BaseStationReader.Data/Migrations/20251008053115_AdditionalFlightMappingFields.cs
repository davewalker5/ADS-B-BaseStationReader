using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AdditionalFlightMappingFields : Migration
    {
        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<string>(
                name: "AirlineICAO",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AlterColumn<string>(
                name: "AirlineIATA",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(string),
                oldType: "TEXT");

            migrationBuilder.AddColumn<string>(
                name: "AirlineName",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AirportIATA",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AirportICAO",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AirportName",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "TEXT",
                nullable: true);

            migrationBuilder.AddColumn<int>(
                name: "AirportType",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<string>(
                name: "Filename",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "TEXT",
                nullable: false,
                defaultValue: "");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AirlineName",
                table: "FLIGHT_NUMBER_MAPPING");

            migrationBuilder.DropColumn(
                name: "AirportIATA",
                table: "FLIGHT_NUMBER_MAPPING");

            migrationBuilder.DropColumn(
                name: "AirportICAO",
                table: "FLIGHT_NUMBER_MAPPING");

            migrationBuilder.DropColumn(
                name: "AirportName",
                table: "FLIGHT_NUMBER_MAPPING");

            migrationBuilder.DropColumn(
                name: "AirportType",
                table: "FLIGHT_NUMBER_MAPPING");

            migrationBuilder.DropColumn(
                name: "Filename",
                table: "FLIGHT_NUMBER_MAPPING");

            migrationBuilder.AlterColumn<string>(
                name: "AirlineICAO",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<string>(
                name: "AirlineIATA",
                table: "FLIGHT_NUMBER_MAPPING",
                type: "TEXT",
                nullable: false,
                defaultValue: "",
                oldClrType: typeof(string),
                oldType: "TEXT",
                oldNullable: true);
        }
    }
}
