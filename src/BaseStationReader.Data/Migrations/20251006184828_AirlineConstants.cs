using System.Diagnostics.CodeAnalysis;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    [ExcludeFromCodeCoverage]
    public partial class AirlineConstants : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AIRLINE_CONSTANTS",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    ConstantDelta = table.Column<int>(type: "INTEGER", nullable: true),
                    ConstantDeltaPurity = table.Column<decimal>(type: "TEXT", nullable: false),
                    ConstantPrefix = table.Column<string>(type: "TEXT", nullable: true),
                    IdentityRate = table.Column<decimal>(type: "TEXT", nullable: false),
                    AirlineICAO = table.Column<string>(type: "TEXT", nullable: false),
                    AirlineIATA = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AIRLINE_CONSTANTS", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AIRLINE_CONSTANTS");
        }
    }
}
