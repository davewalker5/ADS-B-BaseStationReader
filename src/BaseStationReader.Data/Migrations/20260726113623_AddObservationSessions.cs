using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddObservationSessions : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "SessionId",
                table: "TRACKED_AIRCRAFT",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "SESSION",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    StartedAtUtc = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    ProfileName = table.Column<string>(type: "TEXT", nullable: false),
                    ReceiverLatitude = table.Column<double>(type: "REAL", nullable: true),
                    ReceiverLongitude = table.Column<double>(type: "REAL", nullable: true),
                    ReceiverElevation = table.Column<int>(type: "INTEGER", nullable: true),
                    MinimumAltitude = table.Column<int>(type: "INTEGER", nullable: true),
                    MaximumAltitude = table.Column<int>(type: "INTEGER", nullable: true),
                    MaximumDistance = table.Column<int>(type: "INTEGER", nullable: true),
                    IncludedBehaviours = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SESSION", x => x.Id);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TRACKED_AIRCRAFT_SessionId",
                table: "TRACKED_AIRCRAFT",
                column: "SessionId");

            migrationBuilder.CreateIndex(
                name: "IX_SESSION_StartedAtUtc",
                table: "SESSION",
                column: "StartedAtUtc");

            // SQLite rebuilds TRACKED_AIRCRAFT when the foreign key is added. Normalise legacy rows
            // created by older database versions before copying them into the current NOT NULL schema.
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

            migrationBuilder.AddForeignKey(
                name: "FK_TRACKED_AIRCRAFT_SESSION_SessionId",
                table: "TRACKED_AIRCRAFT",
                column: "SessionId",
                principalTable: "SESSION",
                principalColumn: "Id",
                onDelete: ReferentialAction.Restrict);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropForeignKey(
                name: "FK_TRACKED_AIRCRAFT_SESSION_SessionId",
                table: "TRACKED_AIRCRAFT");

            migrationBuilder.DropTable(
                name: "SESSION");

            migrationBuilder.DropIndex(
                name: "IX_TRACKED_AIRCRAFT_SessionId",
                table: "TRACKED_AIRCRAFT");

            migrationBuilder.DropColumn(
                name: "SessionId",
                table: "TRACKED_AIRCRAFT");
        }
    }
}
