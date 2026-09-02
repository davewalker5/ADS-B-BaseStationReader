using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddAirlineCallsignPrefixes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIRLINE_CALLSIGN_PREFIX",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Prefix = table.Column<string>(type: "TEXT", maxLength: 8, nullable: false),
                    AirlineId = table.Column<int>(type: "INTEGER", nullable: false),
                    ProvenanceId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRLINE_CALLSIGN_PREFIX", x => x.Id);
                    table.ForeignKey(
                        name: "FK_AIRLINE_CALLSIGN_PREFIX_AIRLINE_AirlineId",
                        column: x => x.AirlineId,
                        principalTable: "AIRLINE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_AIRLINE_CALLSIGN_PREFIX_PROVENANCE_ProvenanceId",
                        column: x => x.ProvenanceId,
                        principalTable: "PROVENANCE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(
                name: "IX_AIRLINE_CALLSIGN_PREFIX_AirlineId",
                table: "AIRLINE_CALLSIGN_PREFIX",
                column: "AirlineId");

            migrationBuilder.CreateIndex(
                name: "IX_AIRLINE_CALLSIGN_PREFIX_Prefix",
                table: "AIRLINE_CALLSIGN_PREFIX",
                column: "Prefix",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AIRLINE_CALLSIGN_PREFIX_ProvenanceId",
                table: "AIRLINE_CALLSIGN_PREFIX",
                column: "ProvenanceId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIRLINE_CALLSIGN_PREFIX");
        }
    }
}
