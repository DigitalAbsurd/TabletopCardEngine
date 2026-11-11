using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.SqlServer.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddAntes : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<decimal>(
                name: "Ante",
                schema: "tce_holdem",
                table: "Hand",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<decimal>(
                name: "Ante",
                schema: "tce_holdem",
                table: "Game",
                type: "decimal(18,2)",
                precision: 18,
                scale: 2,
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "AnteType",
                schema: "tce_holdem",
                table: "Game",
                type: "nvarchar(32)",
                maxLength: 32,
                nullable: false,
                defaultValue: "Classic");

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
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
                schema: "tce_holdem",
                table: "Hand");

            migrationBuilder.DropColumn(
                name: "Ante",
                schema: "tce_holdem",
                table: "Game");

            migrationBuilder.DropColumn(
                name: "AnteType",
                schema: "tce_holdem",
                table: "Game");

            migrationBuilder.UpdateData(
                schema: "tce_holdem",
                table: "__TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.3.0", 5 });
        }
    }
}
