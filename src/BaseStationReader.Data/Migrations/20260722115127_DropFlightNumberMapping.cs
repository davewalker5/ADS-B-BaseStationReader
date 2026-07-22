using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class DropFlightNumberMapping : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "FLIGHT_NUMBER_MAPPING");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "FLIGHT_NUMBER_MAPPING",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    AirlineIATA = table.Column<string>(type: "TEXT", nullable: true),
                    AirlineICAO = table.Column<string>(type: "TEXT", nullable: true),
                    AirlineName = table.Column<string>(type: "TEXT", nullable: true),
                    AirportIATA = table.Column<string>(type: "TEXT", nullable: true),
                    AirportICAO = table.Column<string>(type: "TEXT", nullable: true),
                    AirportName = table.Column<string>(type: "TEXT", nullable: true),
                    AirportType = table.Column<int>(type: "INTEGER", nullable: false),
                    Callsign = table.Column<string>(type: "TEXT", nullable: false),
                    Destination = table.Column<string>(type: "TEXT", nullable: false),
                    Embarkation = table.Column<string>(type: "TEXT", nullable: false),
                    Filename = table.Column<string>(type: "TEXT", nullable: false),
                    FlightIATA = table.Column<string>(type: "TEXT", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_FLIGHT_NUMBER_MAPPING", x => x.Id);
                });
        }
    }
}
