using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.Sqlite.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddGameTableFormat : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "TableFormat",
                table: "tce_holdem_Game",
                type: "TEXT",
                maxLength: 32,
                nullable: false,
                defaultValue: "SixMax");

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.1.0", 2 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "TableFormat",
                table: "tce_holdem_Game");

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.0.0", 1 });
        }
    }
}
