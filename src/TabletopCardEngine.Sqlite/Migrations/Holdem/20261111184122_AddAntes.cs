using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.Sqlite.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddAntes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Ante",
                table: "tce_holdem_Hand",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Ante",
                table: "tce_holdem_Game",
                type: "TEXT",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnteType",
                table: "tce_holdem_Game",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "Classic");

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.4.0", 6 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Ante",
                table: "tce_holdem_Hand");

            migrationBuilder.DropColumn(
                name: "Ante",
                table: "tce_holdem_Game");

            migrationBuilder.DropColumn(
                name: "AnteType",
                table: "tce_holdem_Game");

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.3.0", 5 });
        }
    }
}
