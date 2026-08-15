using BaseStationReader.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    [DbContext(typeof(BaseStationReaderDbContext))]
    [Migration("20260815120000_AddEquipmentRegister")]
    public partial class AddEquipmentRegister : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "EQUIPMENT_TYPE",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false)
                },
                constraints: table => table.PrimaryKey("PK_EQUIPMENT_TYPE", x => x.Id));

            migrationBuilder.CreateTable(
                name: "EQUIPMENT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    Name = table.Column<string>(type: "TEXT", nullable: false),
                    EquipmentTypeId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EQUIPMENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_EQUIPMENT_EQUIPMENT_TYPE_EquipmentTypeId",
                        column: x => x.EquipmentTypeId,
                        principalTable: "EQUIPMENT_TYPE",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                });

            migrationBuilder.CreateIndex(name: "IX_EQUIPMENT_Name", table: "EQUIPMENT", column: "Name", unique: true);
            migrationBuilder.CreateIndex(name: "IX_EQUIPMENT_EquipmentTypeId", table: "EQUIPMENT", column: "EquipmentTypeId");
            migrationBuilder.CreateIndex(name: "IX_EQUIPMENT_TYPE_Name", table: "EQUIPMENT_TYPE", column: "Name", unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(name: "EQUIPMENT");
            migrationBuilder.DropTable(name: "EQUIPMENT_TYPE");
        }
    }
}
