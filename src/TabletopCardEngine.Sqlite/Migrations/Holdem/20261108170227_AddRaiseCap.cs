using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TabletopCardEngine.Sqlite.Migrations.Holdem
{
    /// <inheritdoc />
    internal partial class AddRaiseCap : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<int>(
                name: "RaiseCap",
                table: "tce_holdem_Game",
                type: "INTEGER",
                nullable: true);

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.3.0", 5 });
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "RaiseCap",
                table: "tce_holdem_Game");

            migrationBuilder.UpdateData(
                table: "tce_holdem___TceSchema",
                keyColumn: "Id",
                keyValue: 1,
                columns: new[] { "EngineVersion", "SchemaVersion" },
                values: new object[] { "1.2.0", 4 });
        }
    }
}
