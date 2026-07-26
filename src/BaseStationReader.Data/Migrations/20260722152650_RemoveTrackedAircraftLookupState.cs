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
            // SQLite rebuilds TRACKED_AIRCRAFT when the obsolete columns are removed. Normalise legacy
            // rows before the first rebuild copies them into the current NOT NULL schema.
            migrationBuilder.Sql(
                """
                UPDATE TRACKED_AIRCRAFT
                SET Messages = COALESCE(Messages, 0),
                    Status = COALESCE(Status, 0),
                    FirstSeen = COALESCE(FirstSeen, LastSeen, CURRENT_TIMESTAMP)
                WHERE Messages IS NULL
                   OR Status IS NULL
                   OR FirstSeen IS NULL;
                """);

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
