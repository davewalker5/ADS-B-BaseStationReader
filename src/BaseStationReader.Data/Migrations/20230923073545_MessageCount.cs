using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class MessageCount : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "Messages",
                table: "AIRCRAFT",
                type: "INTEGER",
                nullable: false,
                defaultValue: 0);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Messages",
                table: "AIRCRAFT");
        }
    }
}
