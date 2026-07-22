using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class RemoveTrackedAircraftLookupState : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "LookupAttempts",
                table: "TRACKED_AIRCRAFT");

            migrationBuilder.DropColumn(
                name: "LookupTimestamp",
                table: "TRACKED_AIRCRAFT");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "LookupAttempts",
                table: "TRACKED_AIRCRAFT",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<DateTime>(
                name: "LookupTimestamp",
                table: "TRACKED_AIRCRAFT",
                type: "TEXT",
                nullable: true);
        }
    }
}
