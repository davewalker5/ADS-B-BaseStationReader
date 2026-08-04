using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddPositionDensitySnapshots : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "POSITION_DENSITY_SNAPSHOT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false),
                    CapturedAtUtc = table.Column<DateTime>(type: "DATETIME", nullable: false),
                    PositionCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MaximumBinCount = table.Column<int>(type: "INTEGER", nullable: false),
                    MinimumLatitude = table.Column<double>(type: "REAL", nullable: false),
                    MaximumLatitude = table.Column<double>(type: "REAL", nullable: false),
                    MinimumLongitude = table.Column<double>(type: "REAL", nullable: false),
                    MaximumLongitude = table.Column<double>(type: "REAL", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POSITION_DENSITY_SNAPSHOT", x => x.Id);
                    table.CheckConstraint("CK_POSITION_DENSITY_SNAPSHOT_LatitudeBounds", "MaximumLatitude >= MinimumLatitude");
                    table.CheckConstraint("CK_POSITION_DENSITY_SNAPSHOT_LongitudeBounds", "MaximumLongitude >= MinimumLongitude");
                    table.CheckConstraint("CK_POSITION_DENSITY_SNAPSHOT_MaximumBinCount", "MaximumBinCount >= 0");
                    table.CheckConstraint("CK_POSITION_DENSITY_SNAPSHOT_PositionCount", "PositionCount >= 0");
                    table.ForeignKey(
                        name: "FK_POSITION_DENSITY_SNAPSHOT_SESSION_SessionId",
                        column: x => x.SessionId,
                        principalTable: "SESSION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "POSITION_DENSITY_SNAPSHOT_CELL",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    PositionDensitySnapshotId = table.Column<int>(type: "INTEGER", nullable: false),
                    Latitude = table.Column<double>(type: "REAL", nullable: false),
                    Longitude = table.Column<double>(type: "REAL", nullable: false),
                    Count = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_POSITION_DENSITY_SNAPSHOT_CELL", x => x.Id);
                    table.CheckConstraint("CK_POSITION_DENSITY_SNAPSHOT_CELL_Count", "Count > 0");
                    table.ForeignKey(
                        name: "FK_POSITION_DENSITY_SNAPSHOT_CELL_POSITION_DENSITY_SNAPSHOT_PositionDensitySnapshotId",
                        column: x => x.PositionDensitySnapshotId,
                        principalTable: "POSITION_DENSITY_SNAPSHOT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_POSITION_DENSITY_SNAPSHOT_SessionId_CapturedAtUtc",
                table: "POSITION_DENSITY_SNAPSHOT",
                columns: new[] { "SessionId", "CapturedAtUtc" });

            migrationBuilder.CreateIndex(
                name: "IX_POSITION_DENSITY_SNAPSHOT_CELL_PositionDensitySnapshotId_Latitude_Longitude",
                table: "POSITION_DENSITY_SNAPSHOT_CELL",
                columns: new[] { "PositionDensitySnapshotId", "Latitude", "Longitude" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "POSITION_DENSITY_SNAPSHOT_CELL");

            migrationBuilder.DropTable(
                name: "POSITION_DENSITY_SNAPSHOT");
        }
    }
}
