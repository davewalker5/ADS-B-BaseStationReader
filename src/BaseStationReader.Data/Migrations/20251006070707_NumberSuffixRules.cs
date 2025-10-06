using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class NumberSuffixRules : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "NUMBER_SUFFIX",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Numeric = table.Column<string>(type: "TEXT", nullable: false),
                    Suffix = table.Column<string>(type: "TEXT", nullable: false),
                    Digits = table.Column<string>(type: "TEXT", nullable: false),
                    Support = table.Column<int>(type: "INTEGER", nullable: false),
                    Purity = table.Column<decimal>(type: "TEXT", nullable: false),
                    AirlineICAO = table.Column<string>(type: "TEXT", nullable: false),
                    AirlineIATA = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_NUMBER_SUFFIX", x => x.Id);
                });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "NUMBER_SUFFIX");
        }
    }
}
