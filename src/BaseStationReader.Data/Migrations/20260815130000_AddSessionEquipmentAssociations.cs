using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class AddSessionEquipmentAssociations : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "SESSION_EQUIPMENT",
                columns: table => new
                {
                    Id = table.Column<int>(type: "INTEGER", nullable: false)
                        .Annotation("Sqlite:Autoincrement", true),
                    EquipmentId = table.Column<int>(type: "INTEGER", nullable: false),
                    SessionId = table.Column<int>(type: "INTEGER", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SESSION_EQUIPMENT", x => x.Id);
                    table.ForeignKey(
                        name: "FK_SESSION_EQUIPMENT_EQUIPMENT_EquipmentId",
                        column: x => x.EquipmentId,
                        principalTable: "EQUIPMENT",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_SESSION_EQUIPMENT_SESSION_SessionId",
                        column: x => x.SessionId,
                        principalTable: "SESSION",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_SESSION_EQUIPMENT_EquipmentId",
                table: "SESSION_EQUIPMENT",
                column: "EquipmentId");

            migrationBuilder.CreateIndex(
                name: "IX_SESSION_EQUIPMENT_SessionId_EquipmentId",
                table: "SESSION_EQUIPMENT",
                columns: new[] { "SessionId", "EquipmentId" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SESSION_EQUIPMENT");
        }
    }
}
