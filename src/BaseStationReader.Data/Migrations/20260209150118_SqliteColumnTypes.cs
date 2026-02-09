using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BaseStationReader.Data.Migrations
{
    /// <inheritdoc />
    public partial class SqliteColumnTypes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "VerticalRate",
                table: "TRACKED_AIRCRAFT",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Track",
                table: "TRACKED_AIRCRAFT",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "TRACKED_AIRCRAFT",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "TRACKED_AIRCRAFT",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GroundSpeed",
                table: "TRACKED_AIRCRAFT",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Altitude",
                table: "TRACKED_AIRCRAFT",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "POSITION",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "POSITION",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Altitude",
                table: "POSITION",
                type: "REAL",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "TEXT",
                oldNullable: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterColumn<decimal>(
                name: "VerticalRate",
                table: "TRACKED_AIRCRAFT",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Track",
                table: "TRACKED_AIRCRAFT",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "TRACKED_AIRCRAFT",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "TRACKED_AIRCRAFT",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "GroundSpeed",
                table: "TRACKED_AIRCRAFT",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Altitude",
                table: "TRACKED_AIRCRAFT",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Longitude",
                table: "POSITION",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Latitude",
                table: "POSITION",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "REAL",
                oldNullable: true);

            migrationBuilder.AlterColumn<decimal>(
                name: "Altitude",
                table: "POSITION",
                type: "TEXT",
                nullable: true,
                oldClrType: typeof(decimal),
                oldType: "REAL",
                oldNullable: true);
        }
    }
}
