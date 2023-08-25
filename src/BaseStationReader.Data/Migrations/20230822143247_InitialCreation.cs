using System;
using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class InitialCreation : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIRCRAFT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Callsign = table.Column<string>(type: "TEXT", nullable: true),
                    Altitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    GroundSpeed = table.Column<decimal>(type: "TEXT", nullable: true),
                    Track = table.Column<decimal>(type: "TEXT", nullable: true),
                    Latitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    Longitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    VerticalRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    Squawk = table.Column<string>(type: "TEXT", nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "DATETIME", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRCRAFT", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIRCRAFT");
        }
    }
}
