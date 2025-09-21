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
            migrationBuilder.Sql("PRAGMA foreign_keys = ON;");

            migrationBuilder.CreateTable(
                name: "AIRLINE",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ICAO = table.Column<string>(type: "TEXT", nullable: false),
                    IATA = table.Column<string>(type: "TEXT", nullable: false),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRLINE", x => x.Id);
                });

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
                name: "TRACKED_AIRCRAFT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Callsign = table.Column<string>(type: "TEXT", nullable: true),
                    Squawk = table.Column<string>(type: "TEXT", nullable: true),
                    Altitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    GroundSpeed = table.Column<decimal>(type: "TEXT", nullable: true),
                    Track = table.Column<decimal>(type: "TEXT", nullable: true),
                    Latitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    Longitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    Distance = table.Column<double>(type: "REAL", nullable: true),
                    VerticalRate = table.Column<decimal>(type: "TEXT", nullable: true),
                    FirstSeen = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    LastSeen = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    Messages = table.Column<int>(type: "INTEGER", nullable: false),
                    Status = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TRACKED_AIRCRAFT", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "FLIGHT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Number = table.Column<string>(type: "TEXT", nullable: false),
                    ICAO = table.Column<string>(type: "TEXT", nullable: false),
                    IATA = table.Column<string>(type: "TEXT", nullable: false),
                    Embarkation = table.Column<string>(type: "TEXT", nullable: false),
                    Destination = table.Column<string>(type: "TEXT", nullable: false),
                    AirlineId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FLIGHT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_FLIGHT_AIRLINE_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "AIRLINE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "MODEL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    ICAO = table.Column<string>(type: "TEXT", nullable: false),
                    IATA = table.Column<string>(type: "TEXT", nullable: false),
                    ManufacturerId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MODEL", x => x.Id);
                    table.ForeignKey(
                        name: "FK_MODEL_MANUFACTURER_ManufacturerId",
                        column: x => x.ManufacturerId,
                        principalTable: "MANUFACTURER",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateTable(
                name: "POSITION",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Altitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    Latitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    Longitude = table.Column<decimal>(type: "TEXT", nullable: true),
                    Distance = table.Column<double>(type: "REAL", nullable: true),
                    Timestamp = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    TrackedAircraftId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POSITION", x => x.Id);
                    table.ForeignKey(
                        name: "FK_POSITION_TRACKED_AIRCRAFT_TrackedAircraftId",
                        column: x => x.TrackedAircraftId,
                        principalTable: "TRACKED_AIRCRAFT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AIRCRAFT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Address = table.Column<string>(type: "TEXT", nullable: false),
                    Registration = table.Column<string>(type: "TEXT", nullable: false),
                    Manufactured = table.Column<int>(type: "INTEGER", nullable: true),
                    Age = table.Column<int>(type: "INTEGER", nullable: true),
                    ModelId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRCRAFT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIRCRAFT_MODEL_ModelId",
                        column: x => x.ModelId,
                        principalTable: "MODEL",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIRCRAFT_ModelId",
                table: "AIRCRAFT",
                column: "ModelId");

            migrationBuilder.CreateIndex(
                name: "IX_FLIGHT_AirlineId",
                table: "FLIGHT",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_MODEL_ManufacturerId",
                table: "MODEL",
                column: "ManufacturerId");

            migrationBuilder.CreateIndex(
                name: "IX_POSITION_TrackedAircraftId",
                table: "POSITION",
                column: "TrackedAircraftId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIRCRAFT");

            migrationBuilder.DropTable(
                name: "FLIGHT");

            migrationBuilder.DropTable(
                name: "POSITION");

            migrationBuilder.DropTable(
                name: "MODEL");

            migrationBuilder.DropTable(
                name: "AIRLINE");

            migrationBuilder.DropTable(
                name: "TRACKED_AIRCRAFT");

            migrationBuilder.DropTable(
                name: "MANUFACTURER");
        }
    }
}
