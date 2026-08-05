using BaseStationReader.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    [DbContext(typeof(BaseStationReaderDbContext))]
    [Migration("20260805083000_AddObservationSessionName")]
    public partial class AddObservationSessionName : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Name",
                table: "SESSION",
                type: "TEXT",
                maxLength: 100,
                nullable: false,
                defaultValue: "");

            migrationBuilder.Sql(
                "UPDATE SESSION SET Name = strftime('%Y-%m-%d %H:%M:%S', StartedAtUtc) WHERE Name = '';");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(name: "Name", table: "SESSION");
        }
    }
}
